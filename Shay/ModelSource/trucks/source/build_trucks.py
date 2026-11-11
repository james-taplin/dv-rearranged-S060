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
    # Exposed heavy coil, including round wire and seated end turns.
    steps=84 if n>8 else 36;sides=6 if n>8 else 4;rad=.042;wire=.0115;v=[];t=[]
    for j in range(steps+1):
        f=j/steps;q=f*math.tau*3.5
        h=(y1-y0)*f
        centre=np.array([x+rad*math.cos(q),y0+h,z+rad*math.sin(q)])
        tangent=np.array([-rad*math.sin(q)*math.tau*3.5,(y1-y0),rad*math.cos(q)*math.tau*3.5]);tangent/=np.linalg.norm(tangent)
        u=np.array([math.cos(q),0,math.sin(q)]);w=np.cross(tangent,u);w/=np.linalg.norm(w)
        for k in range(sides):v.append(centre+wire*(u*math.cos(k*math.tau/sides)+w*math.sin(k*math.tau/sides)))
    for j in range(steps):
        for k in range(sides):a=j*sides+k;b=j*sides+(k+1)%sides;t.extend([[a,b,b+sides],[a,b+sides,a+sides]])
    for k in range(1,sides-1):t.extend([[0,k+1,k],[steps*sides,steps*sides+k,steps*sides+k+1]])
    return mesh(v,t)


def shoe(side,sign,n,back=False):
    v=[];t=[];angles=np.linspace(-.35,.35,15 if n>8 else 9);m=len(angles)
    # The actual tread runs between X=.708 and .800. Keep the rubbing block
    # across that tread; its cast back and pin project beyond the wheel face.
    xs=[side*.718,side*(.829 if back else .794)]
    radii=[.464,.491] if back else [.411,.461]
    for x in xs:
        for r in radii:
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
            cyl(an+'_journal_'+side,ab,[-.048,0,0],[.048,0,0],.131,inner=.112,supports=['spring base','horn guides','native axle through bore'],category='axlebox')
            cyl(an+'_bearing_cap_'+side,ab,[s*.049,0,0],[s*.067,0,0],.140,inner=.112,material='shaft_steel',supports=['journal housing flange'],category='axlebox')
            for zsg in [-1,1]:
                cube(an+'_horn_'+side+str(zsg),root,[s*.52-.047,AY-.01,zz+zsg*.180-.023],[s*.52+.047,AY+.425,zz+zsg*.180+.023],supports=['cast side web','journal sliding guide'],category='frame')
                cyl(an+'_cap_bolt_'+side+str(zsg),ab,[s*.063,.09,zsg*.085],[s*.079,.09,zsg*.085],.014,supports=['cap flange'],material='shaft_steel',category='detail')
            # Two prominent seated coils over each supported inboard axlebox.
            # The entire coil lies above the wheel silhouette and above the
            # bevel ring. Springs and shoes are truck-owned, never wheel-spun.
            cube(an+'_spring_saddle_riser_'+side,ab,[-.050,.128,-.054],[.050,.428,.054],supports=['journal upper shoulders','lower coil seat'],category='axlebox')
            cx=-s*.21
            cube(an+'_coil_lower_seat_'+side,ab,[min(-.069,cx-.08),.424,-.151],[max(.069,cx+.08),.442,.151],supports=['journal saddle','both coil ends'],category='spring')
            for zzs in [-.087,.087]:
                add(an+'_exposed_coil_'+side+str(zzs),ab,lambda n,zzs=zzs,s=s:spring(-s*.21,zzs,.450,.580,n),material='spring_steel',supports=['lower axlebox seat','upper truck seat'],category='spring_smooth')
                cyl(an+'_coil_guide_'+side+str(zzs),ab,[cx,.433,zzs],[cx,.591,zzs],.012,material='shaft_steel',supports=['lower seat','upper retainer'],category='spring')
            cube(an+'_coil_upper_seat_'+side,ab,[cx-.08,.590,-.205],[cx+.08,.610,.205],material='spring_steel',supports=['coil end turns','frame posts'],category='spring')
            for end in [-1,1]:
                lo,hi=sorted([s*.31,s*.58]);ez=zz+end*.180
                cube(an+'_spring_outrigger_'+side+str(end),root,[lo,AY+.420,ez-.025],[hi,AY+.448,ez+.025],supports=['original side web','horn guide'],category='frame')
                cube(an+'_spring_upper_post_'+side+str(end),ab,[cx-.049,.420,end*.180-.024],[cx+.049,.599,end*.180+.024],supports=['outrigger','upper spring seat'],category='spring')
            # Inboard cup ear and riser: accessible from the open bay; not on wheel.
            cube(an+'_oil_ear_'+side,ab,[-s*.09-.07,.105,-.035],[-s*.09+.07,.135,.035],supports=['journal inner side wall, 44 mm overlap in X'],category='axlebox')
            cyl(an+'_oil_riser_'+side,ab,[-s*.12,.125,0],[-s*.12,.185,0],.014,supports=['oil ear'],category='detail')
            marker('OIL_CUP_'+str(ai+1)+'_'+side,ab,[-s*.12,.229,0],purpose='upright oil-cup seat on the riser top; runtime places native cups (R2: five chosen seats)')
        # One substantial tread shoe per wheel, with cast backing, outer
        # hangers and a cross beam beyond the flange sweep between the two wheelsets.
        sg=1 if ai%2==0 else -1;carrier=node(an+'_BRAKE',root,[0,0,zz],'released carrier; static visual rigging follows truck')
        for side_sign in [-1,1]:
            side='L' if side_sign<0 else 'R';xx=side_sign*.846
            add(an+'_shoe_'+side,carrier,lambda n,s=side_sign,sg=sg:shoe(s,sg,n),material='brake_iron',supports=['cast shoe back','brake beam'],category='brake')
            add(an+'_shoe_back_'+side,carrier,lambda n,s=side_sign,sg=sg:shoe(s,sg,n,True),material='spring_steel',supports=['rubbing block','hanger pin','cross beam'],category='brake')
            cube(an+'_shoe_lug_'+side,carrier,[xx-.028,AY-.035,sg*.495-.019],[xx+.028,AY+.035,sg*.495+.019],supports=['shoe casting','beam'],category='brake')
            cyl(an+'_brake_hanger_'+side,carrier,[xx,AY+.385,sg*.300],[xx,AY,sg*.495],.022,material='spring_steel',supports=['cantilever hanger bracket','shoe pin'],category='brake')
            for y,z in [(AY+.385,sg*.300),(AY,sg*.495)]:
                cyl(an+'_brake_pin_'+side+str(y),carrier,[xx-.037,y,z],[xx+.037,y,z],.028,material='shaft_steel',supports=['hanger eye','beam or top clevis'],category='brake')
            lo,hi=sorted([side_sign*.51,side_sign*.88])
            cube(an+'_hanger_top_bracket_'+side,root,[lo,AY+.36,zz+sg*.30-.035],[hi,AY+.405,zz+sg*.30+.035],supports=['end frame web','hanger clevis'],category='frame')
            cyl(an+'_hanger_brace_'+side,root,[side_sign*.54,AY+.435,zz+sg*.19],[xx,AY+.385,zz+sg*.30],.019,material='spring_steel',supports=['end transom','top bracket'],category='frame')
        cyl(an+'_brake_beam',carrier,[-.87,AY,sg*.500],[.87,AY,sg*.500],.024,material='spring_steel',supports=['both hanger eyes','shoe lugs'],category='brake')
        cyl(an+'_pull_link',carrier,[0,AY,sg*.500],[0,AY+.255,sg*.16],.016,material='shaft_steel',supports=['brake beam','equalizer'],category='brake')
        cube(an+'_equalizer',carrier,[-.19,AY+.235,sg*.16-.024],[.19,AY+.275,sg*.16+.024],material='spring_steel',supports=['pull rod','supported pivot'],category='brake')
        cyl(an+'_equalizer_mount',root,[0,AY+.255,zz+sg*.16],[0,AY+.365,zz+sg*.16],.026,supports=['equalizer','cross tie'],category='frame')
        cube(an+'_equalizer_cross_tie',root,[-.55,AY+.35,zz+sg*.16-.025],[.55,AY+.385,zz+sg*.16+.025],supports=['both cast webs','equalizer pivot'],category='frame')
        marker(an+'_BRAKE_ACTUATION',carrier,[0,AY+.255,sg*.16],(0,0,-sg),'static connected visual brake rigging; native brakes retain operation')
    # A cylinder, piston neck, transverse operating shaft and two pull rods
    # connect the end brake beams. No independent brake simulation is added.
    cyl(label+'_brake_cylinder',frame,[-.30,AY+.26,mid-.15],[-.30,AY+.26,mid+.10],.085,supports=['bolted cylinder saddle'],category='pressure_vessel')
    for zz in [mid-.15,mid+.10]:
        cyl(label+'_brake_cylinder_head_'+str(zz),frame,[-.30,AY+.26,zz-.013],[-.30,AY+.26,zz+.013],.099,material='shaft_steel',supports=['cylinder barrel'],category='detail')
    cube(label+'_brake_cylinder_saddle',frame,[-.42,AY+.31,mid-.12],[-.18,AY+.365,mid+.06],supports=['brake barrel','centre transom'],category='frame')
    cyl(label+'_brake_piston',frame,[-.30,AY+.26,mid+.1],[-.30,AY+.26,mid+.24],.021,material='shaft_steel',supports=['cylinder head','operating arm'],category='brake')
    cyl(label+'_brake_crossshaft',frame,[-.52,AY+.26,mid+.24],[.52,AY+.26,mid+.24],.027,material='shaft_steel',supports=['both side frame bearing ears'],category='brake')
    for end,station in [(-1,rear),(1,front)]:
        cyl(label+'_brake_reachrod_'+str(end),frame,[.17*end,AY+.26,mid+.24],[.17*end,AY+.255,station-end*.16],.013,material='shaft_steel',supports=['crossshaft crank','end equalizer'],category='brake')
    # Tapered transverse bolster ends show above the inter-wheel gap and enter
    # the native centre-bearing stack, keeping the moving and rigid load paths.
    for side_sign in [-1,1]:
        lo,hi=sorted([side_sign*.50,side_sign*.84])
        add(label+'_bolster_cheek_'+str(side_sign),frame,lambda n,lo=lo,hi=hi:extrude_x(lo,hi,[(AY+.40,mid-.11),(AY+.485,mid-.07),(AY+.485,mid+.07),(AY+.40,mid+.11)]),supports=['centre bolster','visible outer spring frame'],category='bolster')


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


# Rigid front air-brake equipment, sized for the actual S060 front pocket.
# The running game retains its native compressor and brake simulation.
def barrel_y(cx,cz,stations,radii,n):
    v=[[cx+r*math.cos(a),y,cz+r*math.sin(a)] for y,r in zip(stations,radii) for a in np.arange(n)*math.tau/n];t=[]
    for ring in range(len(stations)-1):
        for i in range(n):
            j=(i+1)%n;a=ring*n+i;b=ring*n+j;t.extend([[a,b,b+n],[a,b+n,a+n]])
    for end in [0,len(stations)-1]:
        at=len(v);v.append([cx,stations[end],cz])
        for i in range(n):t.append([at,end*n+i,end*n+(i+1)%n] if end else [at,(i+1)%n,i])
    return mesh(v,t)

def service(name,points,r=.016,material='pipe_copper'):
    # Rounded centreline with a parallel-transport tube at each detail level.
    points=[np.array(p,float) for p in points];centres=[points[0]]
    for i in range(1,len(points)-1):
        cut=min(.10,np.linalg.norm(points[i]-points[i-1])*.35,np.linalg.norm(points[i+1]-points[i])*.35)
        a=points[i]+(points[i-1]-points[i])/np.linalg.norm(points[i-1]-points[i])*cut
        b=points[i]+(points[i+1]-points[i])/np.linalg.norm(points[i+1]-points[i])*cut
        for q in np.linspace(0,1,7):centres.append((1-q)**2*a+2*q*(1-q)*points[i]+q*q*b)
    centres.append(points[-1])
    def sweep(n):
        v=[];t=[];old_t=None;old_u=None
        for i,c in enumerate(centres):
            tangent=centres[min(i+1,len(centres)-1)]-centres[max(i-1,0)];tangent/=np.linalg.norm(tangent)
            if old_t is None:u=np.cross(tangent,[0,1,0] if abs(tangent[1])<.9 else [1,0,0]);u/=np.linalg.norm(u)
            else:
                u=old_u-tangent*np.dot(old_u,tangent);u/=np.linalg.norm(u)
            w=np.cross(tangent,u);old_t=tangent;old_u=u
            for q in np.arange(n)*math.tau/n:v.append(c+r*(u*math.cos(q)+w*math.sin(q)))
            if i:
                for k in range(n):a=(i-1)*n+k;b=(i-1)*n+(k+1)%n;t.extend([[a,b,b+n],[a,b+n,a+n]])
        for k in range(1,n-1):t.extend([[0,k+1,k],[(len(centres)-1)*n,(len(centres)-1)*n+k,(len(centres)-1)*n+k+1]])
        return mesh(v,t)
    add(name,detail,sweep,material=material,supports=['explicit equipment ports and pipe clips'],category='service_smooth')

px,pz=1.055,3.29
cube('BODY_compressor_mounting_foot',detail,[.805,.770,3.03],[1.29,.820,3.52],supports=['front frame extension','mounting feet'],category='body_detail')
for z in [3.085,3.475]:
    cube('BODY_compressor_mounting_knee_'+str(z),detail,[.825,.785,z-.030],[.875,.985,z+.030],supports=['pump foot','15 mm seat into native frame sill'],category='body_detail')
    cube('BODY_compressor_bearer_'+str(z),detail,[.70,.735,z-.038],[1.29,.785,z+.038],supports=['retained front saddle','pump mounting foot'],category='body_detail')
    for x in [.86,1.24]:
        cyl('BODY_pump_mount_bolt_'+str((x,z)),detail,[x,.794,z],[x,.840,z],.015,material='shaft_steel',supports=['mounting plate','bearer'],category='body_detail')
add('BODY_pump_air_barrel',detail,lambda n:barrel_y(px,pz,[.82,.845,.87,1.105,1.13,1.15],[.175,.175,.153,.153,.175,.175],n),material='frame_painted',supports=['pump foot'],category='pressure_vessel')
for y in np.linspace(.885,1.095,8):
    cyl('BODY_pump_air_cooling_fin_'+str(y),detail,[px,y-.006,pz],[px,y+.006,pz],.164,material='spring_steel',supports=['air barrel casting'],category='pressure_vessel')
add('BODY_pump_steam_barrel',detail,lambda n:barrel_y(px,pz,[1.30,1.32,1.34,1.49,1.51,1.53],[.135,.135,.116,.116,.135,.135],n),material='frame_painted',supports=['pump crosshead bridge','tie rods'],category='pressure_vessel')
cyl('BODY_pump_piston_neck',detail,[px,1.14,pz],[px,1.32,pz],.025,material='shaft_steel',supports=['air/steam piston glands'],category='pressure_vessel')
for sx in [-1,1]:
    for sz in [-1,1]:
        x,z=px+sx*.087,pz+sz*.087
        cyl('BODY_pump_column_'+str((sx,sz)),detail,[x,1.135,z],[x,1.32,z],.013,material='shaft_steel',supports=['lower/upper head flanges'],category='pressure_vessel')
for y,rad in [(.836,.149),(1.14,.149),(1.31,.109),(1.52,.109)]:
    for i in range(8):
        a=i*math.tau/8;x,z=px+rad*math.cos(a),pz+rad*math.sin(a)
        cyl('BODY_pump_head_stud_'+str((y,i)),detail,[x,y-.014,z],[x,y+.014,z],.012,material='shaft_steel',supports=['head flange'],category='body_detail')
cyl('BODY_pump_intake_body',detail,[px,.97,pz+.145],[px,.97,pz+.23],.042,material='spring_steel',supports=['air barrel intake boss'],category='pressure_vessel')
cyl('BODY_pump_intake_cover',detail,[px,.97,pz+.23],[px,.97,pz+.245],.051,material='shaft_steel',supports=['filter housing'],category='pressure_vessel')
cyl('BODY_pump_governor',detail,[1.20,1.34,3.21],[1.20,1.50,3.21],.035,material='pipe_copper',supports=['steam admission union'],category='pressure_vessel')
service('BODY_pump_steam_service',[(.7052183,1.95,3.1679118),(.96,1.95,3.08),(1.31,1.95,3.08),(1.31,1.45,3.21),(1.20,1.45,3.21),(1.16,1.45,3.29)],.016)
service('BODY_pump_exhaust_service',[(1.17,1.39,3.29),(1.32,1.39,3.48),(1.32,2.85,3.48),(1.31,2.85,3.45)],.019,material='spring_steel')
service('BODY_compressor_air_delivery',[(1.19,.98,3.36),(1.30,.98,3.48),(1.30,.92,2.98),(-1.13,.92,2.98),(-1.13,1.31,2.91)],.016)
for name,z,centre,axis,radius in [('steam',3.06,np.array([1.31,1.65,3.158]),np.array([0,-.5,.13]),.026),('exhaust',3.44,np.array([1.32,1.62,3.48]),np.array([0,1,0]),.032)]:
    axis=axis/np.linalg.norm(axis)
    cube('BODY_service_support_foot_'+name,detail,[.920,1.23,z-.08],[.990,1.265,z+.04],material='shaft_steel',supports=['chassis channel upper flange','diagonal support stay'],category='body_detail')
    cyl('BODY_service_support_stay_'+name,detail,[.947,1.25,z],centre,.012,material='shaft_steel',supports=['bolted foot','pipe collar'],category='service_smooth')
    cyl('BODY_service_pipe_collar_'+name,detail,centre-axis*.018,centre+axis*.018,radius,inner=radius-.011,material='shaft_steel',supports=['pipe','diagonal frame stay'],category='pressure_vessel')
# Additional visible chassis channels, fasteners and bolted support knees.
for side in [-1,1]:
    for za,zb in [(-2.70,-.70),(.80,3.42)]:
        lo,hi=sorted([side*.934,side*.960])
        cube('BODY_outer_chassis_web_'+str((side,za)),detail,[lo,1.09 if side<0 else 1.04,za],[hi,1.25,zb],supports=['native sill','cross bearers'],category='body_detail')
        lo,hi=sorted([side*.880,side*1.015])
        for y in [1.085 if side<0 else 1.035,1.235]:
            cube('BODY_chassis_channel_lip_'+str((side,za,y)),detail,[lo,y,za],[hi,y+.023,zb],supports=['web','cross bearers'],category='body_detail')
        for z in np.arange(za+.10,zb-.05,.21):
            for y in [1.087,1.208]:
                cyl('BODY_chassis_rivet_'+str((side,z,y)),detail,[side*.958,y,z],[side*.972,y,z],.010,material='shaft_steel',supports=['chassis web'],category='body_detail')

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
