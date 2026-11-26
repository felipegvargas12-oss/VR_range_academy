using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VRRangeAcademy.Player
{
    /// <summary>
    /// Gestiona el seguimiento 6DoF y lectura de entradas para los mandos Meta Quest 3 (Touch Plus)
    /// o cualquier controlador compatible con OpenXR.
    /// </summary>
    public class VRControllerTracker : MonoBehaviour
    {
        [Header("Configuración del Nodo XR")]
        [SerializeField] private XRNode xrNode = XRNode.RightHand;

        [Header("Visuales y Fallback")]
        [SerializeField] private GameObject visualModel;
        [SerializeField] private Vector3 fallbackLocalPosition = new Vector3(0.22f, 1.25f, 0.45f);
        [SerializeField] private Vector3 fallbackLocalEulerAngles = new Vector3(10f, 0f, 0f);

        [Header("Estado")]
        [SerializeField] private bool isTracked = false;

        private InputDevice targetDevice;
        private bool wasTriggerPressed = false;
        private bool wasPrimaryPressed = false;
        private bool wasSecondaryPressed = false;

        public XRNode HandNode => xrNode;
        public bool IsTracked => isTracked;

        public void SetNode(XRNode node, Vector3 fallbackPos, Vector3 fallbackRot)
        {
            xrNode = node;
            fallbackLocalPosition = fallbackPos;
            fallbackLocalEulerAngles = fallbackRot;
        }

        private void Awake()
        {
            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0).gameObject;
            }
        }

        private void Update()
        {
            UpdateDeviceTracking();
        }

        private void UpdateDeviceTracking()
        {
            targetDevice = InputDevices.GetDeviceAtXRNode(xrNode);

            if (targetDevice.isValid)
            {
                bool gotPos = targetDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 pos);
                bool gotRot = targetDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion rot);

                if (gotPos && gotRot)
                {
                    transform.localPosition = pos;
                    transform.localRotation = rot;
                    isTracked = true;

                    if (visualModel != null && !visualModel.activeSelf)
                    {
                        visualModel.SetActive(true);
                    }
                    return;
                }
            }

            // Modo Fallback (Editor o sin visor conectado)
            isTracked = false;
            // Solo posicionar en fallback si no ha sido modificado manualmente por el rig
            if (transform.localPosition == Vector3.zero)
            {
                transform.localPosition = fallbackLocalPosition;
                transform.localRotation = Quaternion.Euler(fallbackLocalEulerAngles);
            }
        }

        #region Helpers de Entrada (Input)
        public float GetTrigger()
        {
            if (targetDevice.isValid && targetDevice.TryGetFeatureValue(CommonUsages.trigger, out float val))
            {
                return val;
            }
            return 0f;
        }

        public bool IsTriggerPressed(float threshold = 0.5f)
        {
            return GetTrigger() >= threshold;
        }

        public bool WasTriggerPressedThisFrame(float threshold = 0.5f)
        {
            bool pressed = IsTriggerPressed(threshold);
            bool result = pressed && !wasTriggerPressed;
            wasTriggerPressed = pressed;
            return result;
        }

        public float GetGrip()
        {
            if (targetDevice.isValid && targetDevice.TryGetFeatureValue(CommonUsages.grip, out float val))
            {
                return val;
            }
            return 0f;
        }

        public bool IsGripPressed(float threshold = 0.5f)
        {
            return GetGrip() >= threshold;
        }

        public bool IsPrimaryButtonPressed()
        {
            if (targetDevice.isValid && targetDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool pressed))
            {
                return pressed;
            }
            return false;
        }

        public bool WasPrimaryButtonPressedThisFrame()
        {
            bool pressed = IsPrimaryButtonPressed();
            bool result = pressed && !wasPrimaryPressed;
            wasPrimaryPressed = pressed;
            return result;
        }

        public bool IsSecondaryButtonPressed()
        {
            if (targetDevice.isValid && targetDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool pressed))
            {
                return pressed;
            }
            return false;
        }

        public bool WasSecondaryButtonPressedThisFrame()
        {
            bool pressed = IsSecondaryButtonPressed();
            bool result = pressed && !wasSecondaryPressed;
            wasSecondaryPressed = pressed;
            return result;
        }

        public Vector2 GetThumbstick()
        {
            if (targetDevice.isValid && targetDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
            {
                return axis;
            }
            return Vector2.zero;
        }

        public void SendHaptic(float amplitude = 0.5f, float duration = 0.1f)
        {
            if (targetDevice.isValid)
            {
                targetDevice.SendHapticImpulse(0, amplitude, duration);
            }
        }
        #endregion
    }
}
