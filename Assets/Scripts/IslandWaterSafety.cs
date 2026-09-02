using UnityEngine;

namespace CrystalSprint
{
    // No swimming system is introduced. Wade along the beach, return to the last dry shore in deep sea.
    public sealed class IslandWaterSafety : MonoBehaviour
    {
        private PlayerController player;
        private LumberjackEquipment equipment;
        private Vector3 safe;
        private void Awake(){player=GetComponent<PlayerController>();equipment=GetComponent<LumberjackEquipment>();safe=transform.position;}
        private void Update()
        {
            if(Time.timeScale<=0)return;
            Vector3 p=transform.position;
            if(player.IsGrounded && p.y-1.05f>IslandCoast.SeaLevel+.10f)safe=p;
            if(new Vector2(p.x,p.z).magnitude>55 && p.y-1.05f<IslandCoast.SeaLevel-.85f)
            {player.Warp(safe+Vector3.up*.08f);equipment.ShowNotice("Tiefes Wasser – zurück am sicheren Ufer");}
        }
    }
}
