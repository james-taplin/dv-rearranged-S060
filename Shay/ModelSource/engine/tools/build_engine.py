"""R1 original three-cylinder engine. Engine-owned writes only; shared inputs read-only.
Regenerates JSON recipe, material definitions and local Unity project configuration.
Mesh primitives are original code in mesh_primitives.py. R0 sources are archived.
"""
from mesh_primitives import Mesh,ROOT,ASSET,SLOTS,COLORS,dump,sha
import numpy as np
import math,json
CONTRACT=ROOT.parent/'shared/fit-contract.json'
C=json.loads(CONTRACT.read_text())
# R2: the engine moves as one rigid unit. Geometry stays authored in the R1 frame
# (ORIGIN); the bundle places the root at ORIGIN_R2. R2 contract markers are read
# relative to ORIGIN_R2 so they land on the same engine-local points.
ORIGIN=np.array([1.30,.77,.26]);SHIFT=np.array([-.20,-.25,-.15]);ORIGIN_R2=np.array(C['engine']['origin']);SHIFT=ORIGIN_R2-ORIGIN;R=.15;L=.90;E=-.065
ZS=np.array([p[2] for p in C['engine']['cylinderCenters']])-ORIGIN_R2[2]
def w2(p):return np.array(p)-ORIGIN_R2
CRANK_Z=ZS-.035 # identical rigid offset rods clear drain axes and front gear plane
PISTON_OFFSET=.510
def pose(turns,i):
 a=math.tau*((turns%1)+i/3);x=R*math.sin(a);y=R*math.cos(a);dx=E-x;dy=math.sqrt(L*L-dx*dx)
 return np.array([x,y,CRANK_Z[i]]),np.array([E,y+dy,ZS[i]]),math.degrees(math.atan2(-dx,dy))
def w(p):return np.array(p)-ORIGIN
def build(lod):
 n=24 if lod==0 else 12;pn=12 if lod==0 else 8;parts=[];groups=[]
 def group(name,parent='ROOT',position=(0,0,0),rotation=0,kind='static',cylinder=-1):
  groups.append(dict(name=name,parent=parent,position=list(position),rotationZ=rotation,kind=kind,cylinder=cylinder));return name
 group('FIXED');group('CRANK',kind='crank');group('FRONT_REVERSED',position=w([1.3,.47,.65]),rotation=9,kind='frontReversed')
 def part(name,slot='S060_PAINT',g='FIXED',domain='engine'):
  m=Mesh();parts.append(dict(name=name,slot=SLOTS.index(slot),group=g,mesh=m,domain=domain));return m
 def box(name,a,b,slot='S060_PAINT',g='FIXED',domain='engine'):part(name,slot,g,domain).box(a,b)
 def wb(name,a,b,slot='S060_PAINT',domain='engine'):box(name,w(a),w(b),slot,domain=domain)
 def tube(name,a,b,ro,ri=0,slot='S060_MACHINED',g='FIXED',seg=None):part(name,slot,g).tube(a,b,ro,ri,seg or n)
 def wt(name,a,b,ro,ri=0,slot='S060_MACHINED',seg=None):tube(name,w(a),w(b),ro,ri,slot,seg=seg)
 def pipe(name,points,diam):
  m=part(name,'S060_PIPE')
  for a,b in zip(points,points[1:]):m.tube(w(a),w(b),diam/2,diam*.35,pn)
  # Plain mitred elbows: touching/overlapping pipe walls, no decorative floating joints.
 def bolts(name,y,zc,r,count=8):
  if lod:return
  m=part(name,'S060_MACHINED')
  for k in range(count):
   a=k*math.tau/count;x=1.235+r*math.cos(a);z=zc+r*math.sin(a)
   m.tube(w([x,y,z]),w([x,y+.008,z]),.007,0,6)
 # Recessed, original replacement cover and footplate trim. They fit their explicit
 # R1 notch / footplate volumes; they do not pretend to be extracted tank geometry.
 wb('Recessed_notch_cover',[.98,1.16,-.25],[.988,2.65,.87],domain='notchCover')
 for z in [-.23,.85]:wb('Cover_edge_'+str(z),[.988,1.16,z-.008],[1.003,2.65,z+.008],domain='notchCover')
 wb('Footplate_mount_trim',[.98,1.285,-.25],[1.15,1.315,.87],domain='footplateMount')
 wb('Cast_backbone',[.99,1.17,-.23],[1.020,2.43,.87])
 for j,z in enumerate([-.20,.85]):
  wb('Mounting_foot_'+str(j),[1.06,1.285,z-.027],[1.215,1.315,z+.027],domain='footplateMount')
  wb('Cast_support_column_'+str(j),[1.048,1.18,z-.020],[1.09,2.43,z+.020])
  if not lod:
   for x in [1.085,1.19]:wt('Foot_bolt_'+str((j,x)),[x,1.315,z],[x,1.326,z],.009,seg=6)
 # Bearing supports form one local engine bed, not a locomotive frame.
 for j,z in enumerate([.05,.40,.750]):
  wb('Main_bearing_foot_'+str(j),[1.215,.676,z-.035],[1.385,.705,z+.035],'S060_DARK_STEEL')
  wb('Main_bearing_pedestal_'+str(j),[1.235,.700,z-.030],[1.365,.675,z+.030])
  wt('Main_bearing_shell_'+str(j),[1.3,.77,z-.030],[1.3,.77,z+.030],.085,.056,'S060_DARK_STEEL')
  # Inboard hanging bracket joins bed to the fixed column above, clear of rod planes.
  wb('Bearing_hanger_'+str(j),[1.190,.685,z-.025],[1.222,1.32,z+.025])
  wb('Bearing_hanger_bridge_'+str(j),[1.190,.676,z-.030],[1.30,.700,z+.030])
  wb('Bearing_hanger_top_'+str(j),[1.11,1.293,z-.012],[1.222,1.315,z+.012])
 # Interrupted journals and compact twin cheeks. Every true axis is local +Z.
 intervals=[(-.20,-.175),(-.075,.175),(.275,.525),(.625,.84)]
 for j,(za,zb) in enumerate(intervals):tube('Main_journal_'+str(j),w([1.3,.77,za]),w([1.3,.77,zb]),.055,g='CRANK')
 for i,z in enumerate(ZS):
  zz=CRANK_Z[i];phase=group('THROW_'+str(i+1),'CRANK',rotation=-120*i)
  for sign in [-1,1]:
   at=zz+sign*.036;m=part('Crank_cheek_'+str((i,sign)),'S060_DARK_STEEL',phase)
   m.box([-.050,-.050,at-.012],[.050,R+.045,at+.012])
   m.tube([0,0,at-.012],[0,0,at+.012],.062,0,n)
   m.tube([0,R,at-.012],[0,R,at+.012],.050,0,n)
  tube('Crank_pin_'+str(i+1),[0,R,zz-.026],[0,R,zz+.026],.035,g=phase)
  pin,head,angle=pose(0,i);rg=group('ROD_'+str(i+1),position=pin,rotation=angle,kind='rod',cylinder=i)
  dz=ZS[i]-CRANK_Z[i]
  tube('Rod_big_eye_'+str(i+1),[0,0,-.022],[0,0,.022],.062,.036,g=rg)
  tube('Rod_small_eye_'+str(i+1),[0,L,dz-.022],[0,L,dz+.022],.045,.022,g=rg)
  beam=part('Rod_web_'+str(i+1),'S060_MACHINED',rg);beam.box([-.020,.055,-.012],[.020,L-.040,.012])
  # Fixed axial offset is authored INTO each rigid rod; no extra joint or scaling.
  for p in beam.v:p[2]+=dz*p[1]/L
  for normal in beam.n:
   normal[1]-=dz/L*normal[2];length=np.linalg.norm(normal)
   for axis in range(3):normal[axis]/=length
  if not lod:
   for side in [-1,1]:
    flange=part('Rod_I_flange_'+str((i,side)),'S060_MACHINED',rg);flange.box([-.037,.055,side*.018-.005],[.037,L-.040,side*.018+.005])
    for p in flange.v:p[2]+=dz*p[1]/L
    for normal in flange.n:
     normal[1]-=dz/L*normal[2];length=np.linalg.norm(normal)
     for axis in range(3):normal[axis]/=length
  hg=group('CROSSHEAD_'+str(i+1),position=head,kind='crosshead',cylinder=i)
  for side in [-1,1]:box('Crosshead_cheek_'+str((i,side)),[-.060,-.035,side*.036-.012],[.060,.045,side*.036+.012],'S060_DARK_STEEL',hg)
  tube('Wrist_pin_'+str(i+1),[0,0,-.050],[0,0,.050],.022,g=hg)
  tube('Piston_rod_'+str(i+1),[0,.045,0],[0,PISTON_OFFSET,0],.030,g=hg)
  tube('Piston_'+str(i+1),[0,PISTON_OFFSET-.012,0],[0,PISTON_OFFSET+.012,0],.124,g=hg)
  for side in [-1,1]:
   x=1.235+side*.072
   m=part('Guide_'+str((i,side)),'S060_DARK_STEEL')
   for a,b in [(-.080,-.049),(.025,.050)]:m.box(w([x-.016,1.46,ORIGIN[2]+z+a]),w([x+.016,1.92,ORIGIN[2]+z+b]))
  m=part('Guide_lower_yoke_'+str(i+1))
  for a,b in [(-.080,-.049),(.025,.056)]:m.box(w([1.140,1.430,ORIGIN[2]+z+a]),w([1.330,1.460,ORIGIN[2]+z+b]))
  for side in [-1,1]:
   cz=ORIGIN[2]+z+(side*.040 if side>0 else -.056)
   wb('Guide_upper_tie_'+str((i,side)),[1.140,1.905,cz-.008],[1.330,1.935,cz+.008])
  # End castings carry long internal spigots, limiting live chamber volume to the
  # actual piston sweep instead of implying a 500 mm live stroke in the outer jacket.
  cz=ORIGIN[2]+z
  wt('Cylinder_casting_'+str(i+1),[1.235,1.950,cz],[1.235,2.430,cz],.153,.126,'S060_PAINT')
  wt('Lower_head_spigot_'+str(i+1),[1.235,1.950,cz],[1.235,2.000,cz],.1258,.031,'S060_DARK_STEEL')
  wt('Upper_head_spigot_'+str(i+1),[1.235,2.360,cz],[1.235,2.430,cz],.1258,0,'S060_DARK_STEEL')
  for end,yy in [('LOW',1.930),('HIGH',2.430)]:
   wt('Cylinder_head_'+end+str(i+1),[1.235,yy,cz],[1.235,yy+.020,cz],.169,.031 if end=='LOW' else 0,'S060_PAINT')
   bolts('Head_bolts_'+end+str(i+1),yy+.020,cz,.150,count=12)
  wt('Rod_gland_'+str(i+1),[1.235,1.885,cz],[1.235,1.930,cz],.050,.031)
  m=part('Valve_chest_'+str(i+1))
  for lo,hi in [([1.038,2.01,cz-.052],[1.044,2.35,cz+.052]),([1.097,2.01,cz-.052],[1.105,2.35,cz+.052]),([1.046,2.01,cz-.052],[1.097,2.35,cz-.044]),([1.046,2.01,cz+.044],[1.097,2.35,cz+.052]),([1.046,2.34,cz-.044],[1.097,2.35,cz+.044]),([1.046,2.01,cz-.044],[1.0445,2.02,cz+.044]),([1.0575,2.01,cz-.044],[1.097,2.02,cz+.044]),([1.0445,2.01,cz-.044],[1.0575,2.02,cz-.0065]),([1.0445,2.01,cz+.0065],[1.0575,2.02,cz+.044])]:
   if hi[0]>lo[0]:m.box(w(lo),w(hi))
  wb('Valve_cover_'+str(i+1),[1.035,2.00,cz-.058],[1.041,2.36,cz+.058],'S060_DARK_STEEL')
  wt('Valve_gland_'+str(i+1),[1.051,1.98,cz],[1.051,2.01,cz],.014,.0055,'S060_PIPE')
  vg=group('VALVE_VISUAL_ONLY_'+str(i+1),position=w([1.051,1.99,cz]),kind='valveApproximation',cylinder=i)
  tube('Valve_spindle_approx_'+str(i+1),[0,-.065,0],[0,.065,0],.005,g=vg)
  # Frozen six markers: ports on bottom faces of projecting end-head bosses.
  for end,yy in [('REAR',1.930),('FRONT',2.430)]:
   pipe('Drain_end_boss_'+str((i,end)),[[1.375,yy+.010,cz],[1.44,yy+.010,cz],[1.44,yy,cz]],.014)
 # Two substantial top manifolds with short branches, proper supported unions.
 for name,x,y,a,b,diam in [('Steam',1.06,2.55,-.30,.86,.14),('Exhaust',1.43,2.55,-.20,.86,.14)]:
  pipe(name+'_header',[[x,y,a],[x,y,b]],diam)
  for zz in [a,b]:
   wt(name+'_header_flange_'+str(zz),[x,y,zz-.012],[x,y,zz+.012],diam*.76,diam*.36,'S060_MACHINED')
 for i,z in enumerate(ZS):
  cz=ORIGIN[2]+z
  pipe('Steam_branch_'+str(i+1),[[1.06,2.55,cz],[1.066,2.42,cz],[1.066,2.30,cz]],.044)
  pipe('Exhaust_branch_'+str(i+1),[[1.43,2.55,cz+.05],[1.43,2.37,cz+.05],[1.375,2.23,cz+.05]],.054)
  for x in [1.08,1.43]:
   wb('Header_bracket_'+str((i,x)),[min(x,1.37)-.018,2.450,cz-.018],[max(x,1.37)+.018,2.500,cz+.018],'S060_DARK_STEEL')
  # Jacket reinforcing bands and raised cover fasteners retain clear moving bores.
  for yy in [2.04,2.34]:wt('Jacket_band_'+str((i,yy)),[1.235,yy-.010,cz],[1.235,yy+.010,cz],.158,.152,'S060_DARK_STEEL')
  if not lod:
   for yy in [2.05,2.31]:
    for zz in [cz-.042,cz+.042]:
     wt('Valve_cover_bolt_'+str((i,yy,zz)),[1.027,yy,zz],[1.037,yy,zz],.007,seg=6)
 pipe('Steam_inlet_neck',[[1.06,2.55,-.33],[1.20,2.55,-.33]],.14)
 wt('Steam_inlet_flange',[1.185,2.55,-.33],[1.20,2.55,-.33],.095,.050,'S060_MACHINED')
 pipe('Exhaust_outlet_neck',[[1.43,2.55,.70],[1.65,2.55,.70]],.14)
 wt('Exhaust_outlet_flange',[1.635,2.55,.70],[1.65,2.55,.70],.105,.050,'S060_MACHINED')
 # Contract front 20:20 involute pair, pitch centres 150 mm apart, module 7.5 mm.
 # Fitted phase puts an upper gear gap opposite a lower gear tooth at t=0.
 def gear(name,centre,g):
  teeth=20;rp=.150;rb=rp*math.cos(math.radians(20));ra=.165;rf=.131250;invp=math.tan(math.radians(20))-math.radians(20)
  def half(r):
   a=math.acos(rb/r);return math.pi/(2*teeth)+invp-(math.tan(a)-a)
  profile=[];steps=3 if lod==0 else 2
  for k in range(teeth):
   a=k*math.tau/teeth;hb=half(rb)
   profile.append((a-hb-.012,rf))
   for r in np.linspace(rb,ra,steps):profile.append((a-half(float(r)),float(r)))
   for r in np.linspace(ra,rb,steps):profile.append((a+half(float(r)),float(r)))
   profile.append((a+hb+.012,rf))
  m=part(name,'S060_MACHINED',g)
  # Local rotation pivot at true shaft axis. Lower gear has local offset in CRANK.
  c=np.array(centre);width=.040
  for j,(a,r) in enumerate(profile):
   b,s=profile[(j+1)%len(profile)];u=np.array([math.cos(a),math.sin(a),0]);v=np.array([math.cos(b),math.sin(b),0]);z=np.array([0,0,width/2]);h=.019
   p=c+u*r;q=c+v*s;ii=c+u*h;jj=c+v*h
   m.face([p-z,q-z,q+z,p+z]);m.face([ii+z,jj+z,jj-z,ii-z])
   m.face([p+z,q+z,jj+z,ii+z]);m.face([ii-z,jj-z,q-z,p-z])
 gear('Front_reverser_input_20T',w([1.3,.77,.65]),'CRANK')
 gear('Front_reverser_output_20T',[0,0,0],'FRONT_REVERSED')
 box('Input_gear_key',[-.006,.050,.370],[.006,.060,.410],'S060_DARK_STEEL','CRANK')
 box('Output_gear_key',[-.006,.050,-.020],[.006,.060,.020],'S060_DARK_STEEL','FRONT_REVERSED')
 tube('Front_reversed_output_stub',[0,0,-.120],[0,0,.045],.055,g='FRONT_REVERSED')
 wt('Reversed_output_bearing',[1.3,.47,.51],[1.3,.47,.55],.085,.056,'S060_DARK_STEEL')
 wb('Reverser_lower_bearer',[1.30,.36,.51],[1.54,.385,.55],'S060_DARK_STEEL')
 wb('Reverser_outboard_foot',[1.50,.36,.51],[1.54,.40,.73],'S060_DARK_STEEL')
 wb('Reverser_support_post',[1.50,.36,.695],[1.54,1.32,.725],'S060_DARK_STEEL')
 wb('Reverser_upper_bearer',[1.00,1.28,.695],[1.54,1.32,.725],'S060_DARK_STEEL')
 # Bearing housings support the exposed output; no plate crosses its U-joint.
 result=dict(lod=lod,groups=groups,parts=[dict(name=p['name'],slot=p['slot'],group=p['group'],domain=p['domain'],**p['mesh'].data()) for p in parts])
 result['triangles']=sum(len(p['triangles'])//3 for p in result['parts']);return result

def markers():
 out=[]
 def add(name,p,q=(0,0,0,1),diam=None,meaning='',parent='ROOT',alias=None):
  x,y,z,t=q;normal=[2*(x*z+t*y),2*(y*z-t*x),1-2*(x*x+y*y)]
  out.append(dict(name=name,parent=parent,position=list(p),rotationQuaternion=list(q),scale=[1,1,1],normal=normal,diameter=diam,meaning=meaning,contractMarker=alias or name))
 for name in ['ENGINE_MOUNT','ENGINE_CRANK_REAR','ENGINE_CRANK_FRONT','FRONT_DRIVE_OUTPUT','STEAM_IN','EXHAUST_OUT','SMOKEBOX_RETURN']:
  c=next(x for x in C['markers'] if x['name']==name);diam={'ENGINE_CRANK_REAR':.036,'ENGINE_CRANK_FRONT':.036,'FRONT_DRIVE_OUTPUT':.036,'STEAM_IN':.140,'EXHAUST_OUT':.140,'SMOKEBOX_RETURN':.038}.get(name)
  add(name,w2(c['position']),c['rotationQuaternionXYZW'],diam,'Exact frozen R3 interface; pipe diameter is nominal visual outside diameter, not thread or pressure specification')
 for i,z in enumerate(ZS):
  for end,suffix in [('FRONT','HIGH'),('REAR','LOW')]:
   name='CYLINDER_'+str(i+1)+'_DRAIN_'+suffix;c=next(x for x in C['markers'] if x['name']==name)
   add('COCK_C'+str(i+1)+'_'+end,w2(c['position']),c['rotationQuaternionXYZW'],.010,'FRONT = upper end; REAR = lower end. Both downward on lower projecting end-head boss faces. Attachment only.',alias=name)
  add('LUBE_GUIDE_C'+str(i+1),w([1.308,1.90,ORIGIN[2]+z]),q=[-math.sqrt(.5),0,0,math.sqrt(.5)],meaning='stationary guide oil cup')
  add('LUBE_BIG_END_C'+str(i+1),[0,0,.006],meaning='moving rod big-end attachment; native oiling later',parent='ROD_'+str(i+1))
 for j,z in enumerate([.05,.40,.750]):add('LUBE_MAIN_'+str(j+1),w([1.30,.818,z]),q=[-math.sqrt(.5),0,0,math.sqrt(.5)],meaning='stationary main-bearing oil cup')
 return out

def main():
 assert C['status']=='FROZEN_FOR_MODELLING' and C['downstreamModelSessionsMayStart'], 'Final modelling gate is closed'
 assert C['revision']=='S060-SHAY-FIT-R3-20261111'
 lods=[build(0),build(1)];materials=[]
 for i,s in enumerate(SLOTS):materials.append(dict(name=s,contractSlots=[['engine_painted','frame_painted','body_live'],['brake_iron'],['gear_steel','shaft_steel'],['pipe_copper']][i],previewRGBA=COLORS[i]+[1],source='author-owned flat material' if i else 'live active s060_body.sharedMaterial; startup alias LocoS060Green_Body; existing S060OilCups reference refresh pattern',textureFiles=[]))
 model=dict(schema='S060-ENGINE-R3-1',status='MODELLING_R3',bundleRootPosition=ORIGIN_R2.tolist(),finalExportAuthorized=True,fitContractRevision=C['revision'],fitContractSha256=sha(CONTRACT),fitContractStatus=C['status'],coordinateSystem='Unity metres +X right +Y up +Z front; engine root at crank station C2',candidateOverlayOrigin=ORIGIN.tolist(),bodySpaceOrigin=ORIGIN.tolist(),parameters=dict(crankRadius=R,rodLength=L,pistonAxisX=E,cylinderZ=ZS.tolist(),crankZ=CRANK_Z.tolist(),rodAxialOffsets=(ZS-CRANK_Z).tolist(),phasesDegrees=[0,120,240],pistonOffsetAboveCrosshead=PISTON_OFFSET,cylinderBore=.252,nominalCrankStroke=2*R,actualAxisStroke=math.sqrt((L+R)**2-E**2)-math.sqrt((L-R)**2-E**2),valveBaseY=1.99-ORIGIN[1]),materials=materials,markers=markers(),lods=lods)
 ASSET.mkdir(parents=True,exist_ok=True);dump(ASSET/'engine-model.json',model);dump(ROOT/'rig-markers.json',{k:v for k,v in model.items() if k!='lods'});dump(ROOT/'material-slots.json',materials)
 dump(ROOT/'contract-receipt.json',dict(revision=C['revision'],sha256=sha(CONTRACT),source=str(CONTRACT),status=C['status'],usedFor='original engine meshes and marker interfaces; no runtime integration'))
 (ROOT/'unity/ProjectSettings').mkdir(parents=True,exist_ok=True)
 (ROOT/'unity/Packages').mkdir(parents=True,exist_ok=True)
 (ROOT/'unity/ProjectSettings/ProjectVersion.txt').write_text('m_EditorVersion: 2019.4.40f1\nm_EditorVersionWithRevision: 2019.4.40f1 (ffc62b691db5)\n')
 dump(ROOT/'unity/Packages/manifest.json',{'dependencies':{'com.unity.modules.jsonserialize':'1.0.0','com.unity.modules.imgui':'1.0.0','com.unity.modules.imageconversion':'1.0.0'}})
 print(json.dumps(dict(revision=C['revision'],sha256=model['fitContractSha256'],triangles=[x['triangles'] for x in lods],parts=[len(x['parts']) for x in lods]),indent=2))
if __name__=='__main__':main()
