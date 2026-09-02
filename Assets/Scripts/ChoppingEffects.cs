using UnityEngine;
using UnityEngine.Rendering;

namespace CrystalSprint
{
    public static class ChoppingEffects
    {
        public static int HitBursts { get; private set; }
        public static int LandingBursts { get; private set; }
        public static void Burst(Vector3 position, Vector3 direction, Material wood, Material dust, Mesh chip, bool landing = false)
        {
            if(landing) LandingBursts++; else HitBursts++;
            Emit(position,direction,wood,chip,landing?22:12,landing?.10f:.045f,landing?2.5f:1.6f,1f);
            Emit(position,direction,dust,null,landing?18:6,landing?.4f:.11f,landing?.8f:.35f,0f);
        }
        private static void Emit(Vector3 p,Vector3 direction,Material material,Mesh mesh,int count,float size,float speed,float gravity)
        {
            var root=new GameObject(mesh!=null?"Wood chips":"Bark dust"); root.transform.position=p;
            root.transform.rotation=Quaternion.LookRotation((direction+Vector3.up*.65f).normalized);
            var ps=root.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=ps.main; main.playOnAwake=false; main.loop=false;main.duration=.15f; main.startLifetime=new ParticleSystem.MinMaxCurve(.35f,1.1f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(speed*.35f,speed);main.startSize=new ParticleSystem.MinMaxCurve(size*.55f,size);
            main.startRotation3D=true; main.startRotationX=new ParticleSystem.MinMaxCurve(0,6.28f);main.startRotationY=new ParticleSystem.MinMaxCurve(0,6.28f);
            main.gravityModifier=gravity; main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=40;
            var emission=ps.emission; emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)count)});
            var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=55;shape.radius=.045f;
            var color=ps.colorOverLifetime;color.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(mesh!=null?1f:.24f,0),new GradientAlphaKey(0,1)});color.color=gradient;
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(mesh!=null){renderer.renderMode=ParticleSystemRenderMode.Mesh;renderer.mesh=mesh;}
            ps.Play(); Object.Destroy(root,1.5f);
        }
    }
}
