using System;
using UnityEngine;

namespace VRRangeAcademy.Assembly
{
    [RequireComponent(typeof(Collider))]
    public class AssemblyPiece : MonoBehaviour
    {
        [Header("Identificación")]
        public string pieceId = "Slide";
        public string displayName = "Corredera (Slide)";
        public string functionDescription = "Abre y cierra la recámara, aloja el percutor y extrae el casquillo.";

        [Header("Snap Configuration")]
        [Tooltip("Transform del socket de destino en el modelo holográfico")]
        public Transform targetSocket;
        [Tooltip("Distancia en metros para activar el acople automático (snap)")]
        public float snapThresholdDistance = 0.18f;
        [Tooltip("Velocidad de interpolación al encajar")]
        public float snapSmoothSpeed = 15f;

        [Header("Feedback")]
        public AudioClip snapAudioClip;
        public ParticleSystem snapVfx;

        [Header("Estado")]
        [SerializeField] private bool isSnapped = false;
        [SerializeField] private bool isHeld = false;

        public bool IsSnapped => isSnapped;
        public bool IsHeld => isHeld;

        private Rigidbody rb;
        private Collider col;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Transform holdParent;
        private Vector3 holdOffset;
        private Quaternion holdRotOffset;

        public event Action<AssemblyPiece> OnSnapped;
        public event Action<AssemblyPiece> OnGrabbed;
        public event Action<AssemblyPiece> OnReleased;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }

            col = GetComponent<Collider>();
            initialPosition = transform.position;
            initialRotation = transform.rotation;
        }

        private void Update()
        {
            if (isSnapped) return;

            if (isHeld)
            {
                // Seguir al objeto que lo sostiene si existe
                if (holdParent != null)
                {
                    transform.position = holdParent.TransformPoint(holdOffset);
                    transform.rotation = holdParent.rotation * holdRotOffset;
                }

                // Chequear distancia al socket holográfico
                if (targetSocket != null)
                {
                    float dist = Vector3.Distance(transform.position, targetSocket.position);
                    if (dist <= snapThresholdDistance)
                    {
                        ExecuteSnap();
                    }
                }
            }
        }

        public void Grab(Transform holder)
        {
            if (isSnapped) return;

            isHeld = true;
            holdParent = holder;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            holdOffset = holder.InverseTransformPoint(transform.position);
            holdRotOffset = Quaternion.Inverse(holder.rotation) * transform.rotation;

            OnGrabbed?.Invoke(this);
        }

        public void Release()
        {
            if (isSnapped) return;

            isHeld = false;
            holdParent = null;

            if (targetSocket != null)
            {
                float dist = Vector3.Distance(transform.position, targetSocket.position);
                if (dist <= snapThresholdDistance)
                {
                    ExecuteSnap();
                    return;
                }
            }

            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            OnReleased?.Invoke(this);
        }

        public void SetTargetPositionDirect(Vector3 worldPos, Quaternion worldRot)
        {
            if (isSnapped) return;

            transform.position = worldPos;
            transform.rotation = worldRot;

            if (targetSocket != null)
            {
                float dist = Vector3.Distance(transform.position, targetSocket.position);
                if (dist <= snapThresholdDistance)
                {
                    ExecuteSnap();
                }
            }
        }

        public void ExecuteSnap()
        {
            if (isSnapped) return;

            isSnapped = true;
            isHeld = false;
            holdParent = null;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            if (targetSocket != null)
            {
                transform.position = targetSocket.position;
                transform.rotation = targetSocket.rotation;
                transform.SetParent(targetSocket);
            }

            // Reproducir sonido de acople
            if (snapAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(snapAudioClip, transform.position, 1.0f);
            }

            // Partículas
            if (snapVfx != null)
            {
                snapVfx.Play();
            }

            OnSnapped?.Invoke(this);
        }

        public void ResetToTable()
        {
            isSnapped = false;
            isHeld = false;
            holdParent = null;
            transform.SetParent(null);
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        public void SetInitialTransform(Vector3 pos, Quaternion rot)
        {
            initialPosition = pos;
            initialRotation = rot;
            transform.position = pos;
            transform.rotation = rot;
        }
    }
}
