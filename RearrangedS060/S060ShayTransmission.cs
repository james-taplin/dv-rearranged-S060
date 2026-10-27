using System;
using UnityEngine;

// Articulation solver from the original Shay model study. Native bogies own pose;
// the geared native steam engine owns the clock. This class applies visuals only.

namespace RearrangedS060
{
internal sealed class S060ShayTransmission : MonoBehaviour
{
    [Serializable] public class Part { public Transform transform; public string path="", role=""; public int index=0; public Vector3 restPosition=Vector3.zero; public Quaternion restRotation=Quaternion.identity; }
    public double engineTurns;
    public Transform rearTruck, frontTruck;
    public Part[] parts;
    public double rearPhaseErrorDegrees, frontPhaseErrorDegrees, wheelDisagreementDegrees;
    public float rearEngagement, frontEngagement, rearJointAngle, frontJointAngle;
    const double Tau = Math.PI * 2;
    const double G = 43.0 / 19.0;
    readonly double[] output = new double[2];
    readonly Vector3[] a = new Vector3[2], b = new Vector3[2], d = new Vector3[2], u = new Vector3[2], v = new Vector3[2], w = new Vector3[2];
    readonly float[] engagement = new float[2];
    readonly Quaternion[] middle = new Quaternion[2];
    internal double RearWheelTurns => -output[0] / G;
    internal double FrontWheelTurns => output[1] / G;
    public void Evaluate()
    {
        if (rearTruck == null || frontTruck == null || parts == null) return;
        for(int j=0;j<2;j++)
        {
            Transform truck = j==0 ? rearTruck : frontTruck;
            double input = (j==0 ? 1 : -1) * engineTurns;
            double theta = (input % 1) * Tau;
            a[j] = transform.TransformPoint(j==0 ? new Vector3(1.3f,.77f,-.04f) : new Vector3(1.3f,.92f,.57f));
            b[j] = truck.TransformPoint(j==0 ? new Vector3(1.2f,.569703698158f,.599999976158f) : new Vector3(1.2f,.569703698158f,.366999988079f));
            Vector3 axis = (b[j]-a[j]).normalized;
            // Work with the link's positive-Z direction, irrespective of endpoint order.
            d[j] = axis * (j==0 ? -1 : 1);
            u[j] = transform.TransformDirection(new Vector3((float)Math.Cos(theta),(float)Math.Sin(theta),0));
            v[j] = Vector3.Cross(d[j],u[j]).normalized;
            w[j] = Vector3.Cross(v[j],truck.forward).normalized;
            double principal = Math.Atan2(Vector3.Dot(w[j],truck.up),Vector3.Dot(w[j],truck.right))/Tau;
            output[j] = input + Wrap(principal - input);
            middle[j] = Frame(v[j], d[j]);
            engagement[j] = (3*(j==0?.38f:.39f) - (Vector3.Distance(a[j],b[j])-.09f))/2;
            float bend = Mathf.Max(Vector3.Angle(d[j],transform.forward), Vector3.Angle(d[j],truck.forward));
            if(j==0) { rearEngagement=engagement[j];rearJointAngle=bend;rearPhaseErrorDegrees=(output[j]-input)*360; }
            else { frontEngagement=engagement[j];frontJointAngle=bend;frontPhaseErrorDegrees=(output[j]-input)*360; }
        }
        wheelDisagreementDegrees=(-output[0]/G-output[1]/G)*360;
        foreach(var p in parts)
        {
            int j=p.index;
            if(p.role=="outlet") p.transform.localRotation=Quaternion.AngleAxis((float)(((j==0?1:-1)*engineTurns%1)*360),Vector3.forward);
            else if(p.role=="line") p.transform.localRotation=Quaternion.AngleAxis((float)(output[j]%1*360),Vector3.forward);
            else if(p.role=="wheel" || p.role=="pinion")
            {
                j=p.index/2;
                double turns=p.role=="wheel" ? output[j]*(j==0?-1:1)/G : output[j];
                p.transform.localRotation=Quaternion.AngleAxis((float)(turns%1*360),p.role=="wheel"?Vector3.right:Vector3.forward)*p.restRotation;
            }
            else if(p.role=="member")
            {
                j=p.index/3; int member=p.index%3; float length=j==0?.38f:.39f;
                Vector3 direction=(b[j]-a[j]).normalized;
                p.transform.position=a[j]+direction*(.045f+member*(length-engagement[j]));
                // Local +Z goes from body to truck. Cross sections share one rigid phase.
                p.transform.rotation=Frame(v[j],direction);
            }
            else if(p.role=="outer_yoke" || p.role=="inner_yoke" || p.role=="cross")
            {
                j=p.index/2; int end=p.index%2; Transform truck=j==0?rearTruck:frontTruck;
                Vector3 direction=(b[j]-a[j]).normalized;
                p.transform.position=end==0?a[j]:b[j];
                Vector3 trunnion=end==0?u[j]:w[j];
                if(p.role=="cross") p.transform.rotation=Quaternion.LookRotation(Vector3.Cross(trunnion,v[j]),v[j]);
                else if(p.role=="inner_yoke") p.transform.rotation=Frame(v[j],end==0?-direction:direction);
                else
                {
                    // Fork stock points away from the link; local -Z is the stock side.
                    Vector3 outward=(end==0?transform.forward:truck.forward)*(j==0?-1:1)*(end==0?1:-1);
                    p.transform.rotation=Frame(trunnion,outward);
                }
            }
        }
    }
    static Quaternion Frame(Vector3 x,Vector3 z) { return Quaternion.LookRotation(z,Vector3.Cross(z,x).normalized); }
    static double Wrap(double t) { return t-Math.Floor(t+.5); }
}

}
