"""Original parametric Shay truck source. Metres, Unity axes. Writes ONLY trucks/.
No native mesh is copied to a deliverable. Native fixtures are read by validate.py
and the private Unity preview directly from their existing absolute paths.
"""
from pathlib import Path
import json, math, hashlib
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
for folder in ['export','evidence']:(ROOT/folder).mkdir(parents=True,exist_ok=True)
CONTRACT=ROOT.parent/'shared/fit-contract.json'
C=json.loads(CONTRACT.read_text()); HASH=hashlib.sha256(CONTRACT.read_bytes()).hexdigest()
assert C['status']=='FROZEN_FOR_MODELLING' and C['downstreamModelSessionsMayStart']
# R2: generate in the R1 frame (axle .5697) and shift the whole truck down by DROP at
# the end, so only wheel-size parts change; the native support pivot never moves.
AY_R2=C['wheelAxisY']; AY=0.5697036981582642; DROP=AY-AY_R2
GEAR_X=C['bevelGears']['ringPitchPlaneX']; APEX=C['bevelGears']['coneApexX']; SHAFT_R=C['bulk']['lineShaftDiameter']/2
PARTS=[]; NODES=[]; MARKERS=[]; ALIASES=[]; VOLUMES=[]; SUPPORTS=[]

def node(name,parent,position=(0,0,0),kind='group'):
    NODES.append(dict(name=name,parent=parent,position=list(position),kind=kind))
    return name

def marker(name,parent,pos,axis=(0,1,0),purpose='attachment'):
    MARKERS.append(dict(name=name,parent=parent,position=list(map(float,pos)),rotationQuaternionXYZW=[0,0,0,1],axis=list(axis),purpose=purpose,scale=[1,1,1]))

def mesh(v,t):
    v=np.array(v,float); t=np.array(t,int).reshape(-1,3)
    # Authoring coordinates are left-handed Unity coordinates. Outward numeric
    # cross products; renderer corrects handedness with Unity's normal convention.
    volume=np.sum(np.einsum('ij,ij->i',v[t[:,0]],np.cross(v[t[:,1]],v[t[:,2]])))/6
    if volume<0:t=t[:,[0,2,1]]
    return v,t

def box(lo,hi):
    x,y,z=lo;X,Y,Z=hi
    return mesh([[x,y,z],[X,y,z],[X,Y,z],[x,Y,z],[x,y,Z],[X,y,Z],[X,Y,Z],[x,Y,Z]],
        [[0,2,1],[0,3,2],[4,5,6],[4,6,7],[0,1,5],[0,5,4],[3,7,6],[3,6,2],[0,4,7],[0,7,3],[1,2,6],[1,6,5]])

def tube(a,b,r,n=12,inner=0):
    a=np.array(a,float);b=np.array(b,float);d=b-a;d/=np.linalg.norm(d)
    u=np.cross(d,[0,1,0] if abs(d[1])<.9 else [1,0,0]);u/=np.linalg.norm(u);w=np.cross(d,u)
    # Preserve the requested clear bore across LODs: a polygon drawn through
    # the nominal bore radius otherwise pinches between vertices at low LOD.
    radii=[r,inner/math.cos(math.pi/n)] if inner else [r];v=[];t=[]
    for rr in radii:
        for p in [a,b]:
            for q in np.arange(n)*math.tau/n:v.append(p+rr*(u*math.cos(q)+w*math.sin(q)))
    for k in range(n):
        j=(k+1)%n;t.extend([[k,j,n+j],[k,n+j,n+k]])
        if inner:
            t.extend([[2*n+k,3*n+j,2*n+j],[2*n+k,3*n+k,3*n+j],
                [k,2*n+j,j],[k,2*n+k,2*n+j],[n+k,n+j,3*n+j],[n+k,3*n+j,3*n+k]])
    if not inner:
        for k in range(1,n-1):t.extend([[0,k+1,k],[n,n+k,n+k+1]])
    return mesh(v,t)

def beam(a,b,r,n=6):return tube(a,b,r,n)

def extrude_x(x0,x1,poly):
    # Convex y,z polygon, used for bevelled cast side-frame web.
    n=len(poly);v=[[x,y,z] for x in [x0,x1] for y,z in poly];t=[]
    for k in range(1,n-1):t.extend([[0,k+1,k],[n,n+k,n+k+1]])
    for k in range(n):j=(k+1)%n;t.extend([[k,j,n+j],[k,n+j,n+k]])
    return mesh(v,t)

def spring(x,z,y0,y1,n):
    # Helix with its endpoints entering spring seat plates, no floating coil.
    steps=28 if n>8 else 12;rad=.033;wire=.009;v=[];t=[]
    for j in range(steps+1):
        q=j/steps*math.tau*3;center=np.array([x+rad*math.cos(q),y0+(y1-y0)*j/steps,z+rad*math.sin(q)])
        u=np.array([math.cos(q),0,math.sin(q)]);w=np.array([0,1,0])
        for k in range(4):v.append(center+wire*(u*math.cos(k*math.pi/2)+w*math.sin(k*math.pi/2)))
    for j in range(steps):
        for k in range(4):a=j*4+k;b=j*4+(k+1)%4;t.extend([[a,b,b+4],[a,b+4,a+4]])
    t.extend([[0,2,1],[0,3,2],[steps*4,steps*4+1,steps*4+2],[steps*4,steps*4+2,steps*4+3]])
    return mesh(v,t)

def shoe(side,sign,n):
    v=[];t=[];angles=np.linspace(-.145,.145,9 if n>8 else 5);m=len(angles)
    for x in [side*.731,side*.791]:
        for r in [0.407596629617,0.436871525667]:
            for a in angles:v.append([x,AY+r*math.sin(a),sign*r*math.cos(a)])
    for j in range(m-1):
        for a,b in [(0,m),(m,3*m),(3*m,2*m),(2*m,0)]:t.extend([[a+j,a+j+1,b+j+1],[a+j,b+j+1,b+j]])
    for j in [0,m-1]:t.extend([[j,m+j,3*m+j],[j,3*m+j,2*m+j]])
    return mesh(v,t)

def add(name,parent,fn,material='frame_painted',supports=(),category='frame'):
    lod=[]
    for n in [16,8]:
        v,t=fn(n);lod.append(dict(vertices=v.tolist(),triangles=t.flatten().tolist()))
    PARTS.append(dict(name=name,parent=parent,material=material,category=category,lods=lod))
    SUPPORTS.append(dict(part=name,loadPath=list(supports)))

def cube(name,parent,lo,hi,**kw):add(name,parent,lambda n:box(lo,hi),**kw)
def cyl(name,parent,a,b,r,inner=0,**kw):add(name,parent,lambda n:tube(a,b,r,n,inner),**kw)

node('BODY_ROOT','ROOT')
for ti,tr in enumerate(C['trucks']):
    label=tr['name'].upper();pivot=np.array(tr['pivot']);pv=pivot[2]
    root=node('TRUCK_'+label,'BODY_ROOT',pivot,'native bogie_car attachment')
    frame=node(label+'_FRAME',root);marker('TRUCK_PIVOT',root,[0,0,0],purpose='native virtual support; never relocate')
    rear,front=[C['wheelStations'][i]-pv for i in tr['axleIndices']];mid=(rear+front)/2;z0=rear-.22;z1=front+.22
    for s in [-1,1]:
        side='L' if s<0 else 'R';x=s*.52
        poly=[(.865,z0),(.98,z0),(1.02,z0+.05),(1.02,z1-.05),(.98,z1),(.865,z1),(.815,z1-.20),(.815,z0+.20)]
        add(label+'_cast_web_'+side,frame,lambda n,x=x,p=poly:extrude_x(x-.045,x+.045,p),supports=['spring top seats','bolster','end transoms'])
        cube(label+'_top_flange_'+side,frame,[x-.06,.987,z0+.025],[x+.06,1.02,z1-.025],supports=['cast web'])
        cube(label+'_bottom_flange_'+side,frame,[x-.052,.81,z0+.20],[x+.052,.837,z1-.20],supports=['cast web'])
    for j,zz in enumerate([z0+.07,mid,z1-.07]):
        cube(label+'_transom_'+str(j),frame,[-.565,.90,zz-.04],[.565,1.015,zz+.04],supports=['both cast side webs'])
        for s in [-1,1]:
            add(label+'_transom_gusset_'+str(j)+'_'+str(s),frame,lambda n,s=s,zz=zz:extrude_x(s*.47-.035,s*.47+.035,[(.84,zz-.10),(1.01,zz-.10),(1.01,zz+.10)]),supports=['transom','side web'])
    cube(label+'_asymmetric_bolster',frame,[-.58,1.015,-.105],[.58,1.105,.105],supports=['both frame flanges'],category='bolster')
    cube(label+'_bolster_seat_base',frame,[-.22,1.10,-.135],[.22,1.14,.135],supports=['bolster'],category='bolster')
    cyl(label+'_pivot_seat',frame,[0,1.135,0],[0,1.205,0],.15,inner=.047,supports=['bolster seat base'],category='bolster')
    body=node(label+'_BODY_ATTACHMENT','BODY_ROOT',[0,0,pv],'fixed body-local')
    cube(label+'_body_top_plate',body,[-.25,1.27,-.18],[.25,1.295,.18],supports=['retained body at y=1.285; intentional mounting overlap'],category='body_attachment')
    cyl(label+'_body_seat',body,[0,1.25,0],[0,1.275,0],.135,inner=.05,supports=['body top plate'],category='body_attachment')
    marker(label+'_BODY_MOUNT',body,[0,1.285,0],purpose='fixed body attachment; separate from swivel truck')
    # Original supported line-shaft bearing pedestal in clear inter-axle bay.
    cube(label+'_shaft_arm',frame,[.54,.735,mid-.03],[APEX+.03,.78,mid+.03],supports=['right side web via root rib'],category='bracket')
    cube(label+'_shaft_arm_root',frame,[.515,.74,mid-.07],[.58,.94,mid+.07],supports=['right web','shaft arm'],category='bracket')
    cube(label+'_shaft_drop',frame,[APEX-.035,AY+.08,mid-.03],[APEX+.035,.76,mid+.03],supports=['shaft arm','bearing housing'],category='bracket')
    cyl(label+'_shaft_bearing',frame,[APEX,AY,mid-.05],[APEX,AY,mid+.05],SHAFT_R+.035,inner=SHAFT_R+.002,supports=['shaft drop'],category='bracket')
    marker(label+'_LINE_SHAFT_BEARING',root,[APEX,AY,mid],(0,0,1),'supported bearing seat; transmission owns shaft')
    for ai,zz in zip(tr['axleIndices'],[rear,front]):
        an=label+'_AXLE_'+str(ai+1);ax=node(an,root,[0,AY,zz],'wheel spin +X')
        marker('AXLE_SPIN_'+str(ai+1),ax,[0,0,0],(1,0,0),'native PoweredWheel ownership; cosmetic source adds no physics')
        marker('GEAR_SEAT_'+str(ai+1),ax,[GEAR_X,0,0],(1,0,0),'right ring pitch-plane; gear owned by transmission')
        marker('BEVEL_APEX_'+str(ai+1),root,[APEX,AY,zz],(1,0,0),'common cone apex; pinion axis +Z')
        alias=C['wheelReuse']['nativeMeshAliases'][ai]
        wn=node(an+'_NATIVE_SET',ax,kind='native live alias; intentionally empty MeshFilter')
        ALIASES.append(dict(path=wn,parent=ax,ReplacementName=alias,lod1ReplacementName=alias+'_LOD1',scale=C['wheelReuse']['scale'],rotationQuaternionXYZW=[0,0,0,1],axis=[1,0,0],completeNativeMesh=True,completeWheelset=True,meshComponent='live runtime alias',Filter='target MeshFilter',materialAlias='S282 pony wheel material',note='R2: complete vanilla S282 pony wheelset, unscaled.'))
        for s in [-1,1]:
            side='L' if s<0 else 'R'
            ab=node(an+'_AXLEBOX_'+side,root,[s*.52,AY,zz],'nonspinning journal follows truck; suspension owner may move this group')
            cyl(an+'_journal_'+side,ab,[-.064,0,0],[.064,0,0],.143,inner=.085,supports=['spring base','horn guides','native axle through bore'],category='axlebox')
            cyl(an+'_bearing_cap_'+side,ab,[s*.061,0,0],[s*.079,0,0],.148,inner=.085,material='shaft_steel',supports=['journal housing flange'],category='axlebox')
            for zsg in [-1,1]:
                cube(an+'_horn_'+side+str(zsg),root,[s*.52-.048,AY-.01,zz+zsg*.174-.027],[s*.52+.048,.9,zz+zsg*.174+.027],supports=['cast side web','journal sliding guide'],category='frame')
                cyl(an+'_cap_bolt_'+side+str(zsg),ab,[s*.077,.09,zsg*.085],[s*.092,.09,zsg*.085],.014,supports=['cap flange'],material='shaft_steel',category='detail')
            cube(an+'_spring_base_'+side,ab,[-.073,.122,-.125],[.073,.151,.125],supports=['journal casting upper shoulders'],category='axlebox')
            for zs in [-.075,.075]:
                add(an+'_spring_'+side+str(zs),ab,lambda n,zs=zs:spring(0,zs,.147,.267,n),material='shaft_steel',supports=['journal spring base','frame spring top'],category='spring')
            cube(an+'_spring_top_'+side,ab,[-.07,.26,-.126],[.07,.289,.126],supports=['springs','side web at y=.815'],category='spring')
            # Inboard cup ear and riser: accessible from the open bay; not on wheel.
            cube(an+'_oil_ear_'+side,ab,[-s*.09-.07,.105,-.035],[-s*.09+.07,.135,.035],supports=['journal inner side wall, 44 mm overlap in X'],category='axlebox')
            cyl(an+'_oil_riser_'+side,ab,[-s*.12,.125,0],[-s*.12,.185,0],.014,supports=['oil ear'],category='detail')
            marker('OIL_CUP_'+str(ai+1)+'_'+side,ab,[-s*.12,.229,0],purpose='upright oil-cup seat on the riser top; runtime places native cups (R2: five chosen seats)')
        # Two tread shoes outside the truck ends, common cross beam and supported
        # swing arms. Carrier translation is an integration interface only.
        sg=-1 if ai%2==0 else 1;carrier=node(an+'_BRAKE',root,[0,0,zz],'released carrier; applied travel -sign*.012 Z')
        for s in [-1,1]:
            side='L' if s<0 else 'R'
            add(an+'_shoe_'+side,carrier,lambda n,s=s,sg=sg:shoe(s,sg,n),material='brake_iron',supports=['shoe back lug','brake beam'],category='brake')
            cube(an+'_shoe_lug_'+side,carrier,[s*.761-.035,AY-.035,sg*.443-.015],[s*.761+.035,AY+.035,sg*.443+.015],supports=['shoe back','beam'],category='brake')
            cyl(an+'_brake_hanger_'+side,carrier,[s*.54,.94,sg*.19],[s*.54,AY,sg*.448],.018,supports=['truck end transom hanger pin','brake beam'],category='brake')
            cyl(an+'_hanger_pin_'+side,root,[s*.54-.034,.94,zz+sg*.19],[s*.54+.034,.94,zz+sg*.19],.026,supports=['end transom'],category='frame')
        cyl(an+'_brake_beam',carrier,[-.79,AY,sg*.448],[.79,AY,sg*.448],.023,supports=['both hangers','both shoe lugs'],category='brake')
        cyl(an+'_pull_link',carrier,[0,AY,sg*.448],[0,.84,sg*.15],.013,supports=['brake beam','equalizer lever'],category='brake')
        cube(an+'_equalizer',carrier,[-.09,.825,sg*.15-.025],[.09,.855,sg*.15+.025],supports=['pull link','frame-supported pivot'],category='brake')
        cyl(an+'_equalizer_mount',root,[0,.84,zz+sg*.15],[0,.925,zz+sg*.15],.02,supports=['equalizer','frame center transom/bolster via cross tie'],category='frame')
        cube(an+'_equalizer_cross_tie',root,[-.53,.91,zz+sg*.15-.024],[.53,.94,zz+sg*.15+.024],supports=['both cast webs','equalizer pivot'],category='frame')
        for s in [-1,1]:
            lo=[s*.761-.043,AY-.10,zz+sg*.448-.04];hi=[s*.761+.043,AY+.10,zz+sg*.448+.04]
            VOLUMES.append(dict(name=an+'_BRAKE_CLEARANCE_'+str(s),parent=root,min=lo,max=hi,travel=[0,0,-sg*.012],kind='shoe/beam swept reservation; no Collider'))
        marker(an+'_BRAKE_ACTUATION',carrier,[0,.84,sg*.15],(0,0,-sg),'12 mm conservative applied travel; pivoted rigging requires integration articulation')
        VOLUMES.append(dict(name=an+'_BRAKE_BEAM_CLEARANCE',parent=root,min=[-.82,AY-.05,zz+sg*.448-.05],max=[.82,AY+.05,zz+sg*.448+.05],travel=[0,0,-sg*.012],kind='complete beam swept reservation; no Collider'))

ROOTS={n['name'] for n in NODES if n['kind']=='native bogie_car attachment'}
for n in NODES:
    if n['parent'] in ROOTS or n['name'].endswith('_BODY_ATTACHMENT'):n['position'][1]-=DROP
for p in PARTS:
    if p['parent'] in ROOTS:
        for l in p['lods']:
            v=np.array(l['vertices']);v[:,1]-=DROP;l['vertices']=v.tolist()
for m in MARKERS:
    if m['parent'] in ROOTS and m['name']!='TRUCK_PIVOT':m['position'][1]-=DROP
for v in VOLUMES:
    if v['parent'] in ROOTS:v['min'][1]-=DROP;v['max'][1]-=DROP
AY=AY_R2

# R3 rigid body equipment. These stay on the locomotive, above the truck sweep.
# Deep perforated girders, flanges and transverse bearers give the tank a visible
# support structure; they are not plates attached to the swivelling trucks.
detail=node('SHAY_FRAME_DETAILS','BODY_ROOT')
def panel(x0,x1,z0,z1):
    n=24;yc=1.325;zc=(z0+z1)/2;hy=.225;hz=(z1-z0)/2
    outer=[];inner=[]
    for a in np.arange(n)*math.tau/n:
        y,z=math.sin(a),math.cos(a);k=1/max(abs(y)/hy,abs(z)/hz)
        outer.append((yc+y*k,zc+z*k));inner.append((yc+y*.105,zc+z*min(.24,hz-.10)))
    v=[[x,y,z] for x in [x0,x1] for ring in [outer,inner] for y,z in ring];t=[]
    def quad(a,b,c,d):t.extend([[a,b,c],[a,c,d]])
    for i in range(n):
        j=(i+1)%n;quad(i,j,n+j,n+i);quad(2*n+j,2*n+i,3*n+i,3*n+j)
        quad(j,i,2*n+i,2*n+j);quad(n+i,n+j,3*n+j,3*n+i)
    return mesh(v,t)
for side in [-1,1]:
    start=.80 if side>0 else -.55;end=3.42
    divisions=np.linspace(start,end,4 if side>0 else 6)
    for i,(a,b) in enumerate(zip(divisions,divisions[1:])):
        x0,x1=sorted([side*.878,side*.905])
        add('BODY_perforated_girder_'+str((side,i)),detail,lambda n,x0=x0,x1=x1,a=a,b=b:panel(x0,x1,a,b),supports=['frame sill','top flange','tank bearers'],category='body_detail')
        # Proper round rivet heads on the web, with enough land around each hole.
        for z in np.arange(a+.055,b-.025,.14):
            for y in [1.145,1.505]:
                cyl('BODY_web_rivet_'+str((side,i,z,y)),detail,[side*.901,y,z],[side*.915,y,z],.009,material='shaft_steel',supports=['girder web'],category='body_detail')
    for y in [1.09,1.525]:
        lo,hi=sorted([side*.79,side*.982])
        cube('BODY_girder_flange_'+str((side,y)),detail,[lo,y,start],[hi,y+.027,end],supports=['native sill','perforated web'],category='body_detail')
for i,z in enumerate([.82,1.75,2.66,3.36]):
    cube('BODY_tank_bearer_'+str(i),detail,[-.90,1.445,z-.045],[.90,1.555,z+.045],supports=['both girder webs','tank underside'],category='body_detail')
    cube('BODY_bearer_seating_plate_'+str(i),detail,[-1.02,1.55,z-.058],[1.02,1.63,z+.058],supports=['transverse bearer','retained native footplate'],category='body_detail')
    for side in [-1,1]:
        x0,x1=sorted([side*.84,side*.89])
        add('BODY_bearer_knee_'+str((i,side)),detail,lambda n,x0=x0,x1=x1,z=z:extrude_x(x0,x1,[(1.12,z-.11),(1.47,z-.11),(1.47,z+.11)]),supports=['girder','transverse bearer'],category='body_detail')

def reservoir(n):
    stations=[1.70,1.72,1.77,1.82,2.72,2.77,2.82,2.84];radii=[.02,.095,.14,.15,.15,.14,.095,.02]
    v=[[-1.13+rr*math.cos(a),1.31+rr*math.sin(a),z] for z,rr in zip(stations,radii) for a in np.arange(n)*math.tau/n];t=[]
    for ring in range(len(stations)-1):
        for i in range(n):
            j=(i+1)%n;a=ring*n+i;b=ring*n+j;t.extend([[a,b,b+n],[a,b+n,a+n]])
    for end in [0,len(stations)-1]:
        at=len(v);v.append([-1.13,1.31,stations[end]])
        for i in range(n):t.append([at,end*n+i,end*n+(i+1)%n] if end else [at,(i+1)%n,i])
    return mesh(v,t)
add('BODY_domed_air_reservoir',detail,reservoir,material='frame_painted',supports=['two strapped frame saddles'],category='pressure_vessel')
for z in [1.95,2.58]:
    cyl('BODY_reservoir_strap_'+str(z),detail,[-1.13,1.31,z-.022],[-1.13,1.31,z+.022],.16,inner=.149,material='shaft_steel',supports=['reservoir shell','saddle feet'],category='body_detail')
    cube('BODY_reservoir_saddle_'+str(z),detail,[-1.21,1.10,z-.034],[-.88,1.16,z+.034],supports=['lower flange','reservoir strap'],category='body_detail')
    cyl('BODY_strap_stud_'+str(z),detail,[-1.20,1.12,z],[-1.20,1.22,z],.012,material='shaft_steel',supports=['saddle','strap'],category='body_detail')
cyl('BODY_reservoir_drain',detail,[-1.13,1.16,2.27],[-1.13,1.08,2.27],.018,material='pipe_copper',supports=['reservoir bottom boss'],category='body_detail')
cyl('BODY_reservoir_delivery_union',detail,[-1.13,1.31,2.83],[-1.13,1.31,2.91],.025,material='pipe_copper',supports=['domed reservoir end'],category='body_detail')

def abspos(name):
    if name=='ROOT':return np.zeros(3)
    n=next(n for n in NODES if n['name']==name);return np.array(n['position'])+abspos(n['parent'])

recipe=dict(revision=C['revision'],contractSha256=HASH,units='metres',coordinateSystem=C['coordinateSystem'],nodes=NODES,parts=PARTS,markers=MARKERS,nativeAliases=ALIASES,brakeVolumes=VOLUMES,loadPaths=SUPPORTS)
(ROOT/'source/truck-recipe.json').write_text(json.dumps(recipe,separators=(',',':')))
(ROOT/'marker-axes.json').write_text(json.dumps({k:recipe[k] for k in ['revision','contractSha256','units','coordinateSystem','nodes','markers','brakeVolumes']},indent=2))
(ROOT/'native-attachments.json').write_text(json.dumps(dict(revision=C['revision'],contractSha256=HASH,aliases=ALIASES,attachment='attach TRUCK_REAR/FRONT contents to corresponding native bogie_car with identity local transform; move body plates to BODY_ROOT; no Rigidbody/Collider/Joint',materials=dict(logicalSlots=['frame_painted','shaft_steel','brake_iron','pipe_copper'],alias='LocoS060Green_Body',slot=0,refreshRule=C['materials']['refreshRule']),linkage='later conversion owns reversible renderer visibility; retain all integral native bosses/counterweights',servicing='eight decorative markers only; no new point IDs or quantities'),indent=2))

# Interchange OBJ is original geometry only, with split face normals and planar
# metric UVs. Unity .asset meshes generated from the same recipe are authoritative.
counts={}
for li in [0,1]:
    total=0;lines=['# Original Shay truck parts only; Unity metres +X right +Y up +Z front','mtllib logical-slots.mtl'];offset=0
    for p in PARTS:
        m=p['lods'][li];v=np.array(m['vertices'])+abspos(p['parent']);t=np.array(m['triangles']).reshape(-1,3);total+=len(t)
        lines+=['o '+p['name'],'usemtl '+p['material']]
        for tri in v[t]:
            normal=np.cross(tri[1]-tri[0],tri[2]-tri[0]);normal/=np.linalg.norm(normal);axes=np.argsort(np.abs(normal))[:2]
            lines+=['v '+' '.join(f'{a:.8f}' for a in q) for q in tri]
            lines+=['vt '+' '.join(f'{a:.8f}' for a in q[axes]) for q in tri]
            lines+=['vn '+' '.join(f'{a:.8f}' for a in normal)]*3
            inds=[str(offset+i+1) for i in range(3)];lines+=['f '+' '.join(a+'/'+a+'/'+a for a in inds)];offset+=3
    (ROOT/f'export/Shay-trucks-LOD{li}.obj').write_text('\n'.join(lines)+'\n');counts['LOD'+str(li)]=total
(ROOT/'export/logical-slots.mtl').write_text('\n'.join('newmtl '+s+'\nKd '+c+'\n' for s,c in [('frame_painted','.16 .23 .22'),('shaft_steel','.36 .39 .40'),('brake_iron','.19 .18 .17'),('pipe_copper','.52 .31 .14')]))
counts['bothFramesAndCoversLOD0']=sum(len(p['lods'][0]['triangles'])//3 for p in PARTS if p['category'] not in ['brake','body_attachment'])
counts['parts']=len(PARTS);counts['oilCupMarkers']=sum(m['name'].startswith('OIL_CUP') for m in MARKERS)
(ROOT/'evidence/triangle-counts.json').write_text(json.dumps(counts,indent=2));print(json.dumps(counts,indent=2))
