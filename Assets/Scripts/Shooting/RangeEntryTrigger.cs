using UnityEngine;

namespace VRRangeAcademy.Shooting
{
    /// <summary>
    /// Detecta el paso del jugador entre el Taller de Armería y el Polígono de Tiro.
    /// Al ingresar al polígono, equipa automáticamente el arma activa y habilita la línea de fuego.
    /// </summary>
    public class RangeEntryTrigger : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponent<Assembly.AssemblyPlayerController>() 
                         ?? other.GetComponentInParent<Assembly.AssemblyPlayerController>();
            if (player != null)
            {
                var mgr = Object.FindAnyObjectByType<Assembly.WeaponAssemblyManager>();
                if (mgr != null)
                {
                    mgr.EquipActiveWeapon();
                }

                if (ShootingRangeManager.Instance != null && !ShootingRangeManager.Instance.IsRangeUnlocked)
                {
                    ShootingRangeManager.Instance.UnlockRange("Arma de Capacitación");
                }
            }
        }
    }
}
