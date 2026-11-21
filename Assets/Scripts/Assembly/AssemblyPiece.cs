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
        public AudioClip unsnapAudioClip;
        public ParticleSystem snapVfx;

        [Header("Estado")]
        [SerializeField] private bool isSnapped = false;
        [SerializeField] private bool isHeld = false;
        private bool canSnapAgain = true;

        public bool IsSnapped => isSnapped;
        public bool IsHeld => isHeld;
        public Transform TargetSocket => targetSocket;

        private Rigidbody rb;
        private Collider col;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Transform holdParent;
        private Vector3 holdOffset;
        private Quaternion holdRotOffset;

        public event Action<AssemblyPiece> OnSnapped;
        public event Action<AssemblyPiece> OnUnsnapped;
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
                if (holdParent != null)
                {
                    transform.position = holdParent.TransformPoint(holdOffset);
                    transform.rotation = holdParent.rotation * holdRotOffset;
                }

                // Chequear distancia al socket holográfico
                if (targetSocket != null)
                {
                    float dist = Vector3.Distance(transform.position, targetSocket.position);
                    if (dist > snapThresholdDistance * 1.5f)
                    {
                        canSnapAgain = true;
                    }

                    if (canSnapAgain && dist <= snapThresholdDistance)
                    {
                        ExecuteSnap();
                    }
                }
            }
        }

        public void Grab(Transform holder)
        {
            if (isSnapped)
            {
                DetachFromSocket();
            }

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

        public void DetachFromSocket()
        {
            if (!isSnapped) return;

            isSnapped = false;
            canSnapAgain = false;
            transform.SetParent(null);

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (unsnapAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(unsnapAudioClip, transform.position, 1.0f);
            }
            else if (snapAudioClip != null)
            {
                AudioSource.PlayClipAtPoint(snapAudioClip, transform.position, 0.9f);
            }

            OnUnsnapped?.Invoke(this);
        }

        public void SnapToSocketImmediate()
        {
            isSnapped = true;
            isHeld = false;
            holdParent = null;
            canSnapAgain = false;

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            if (targetSocket != null)
            {
                transform.position = targetSocket.position;
                transform.rotation = targetSocket.rotation;
                transform.SetParent(targetSocket);
            }
        }

        public void Release()
        {
            if (isSnapped) return;

            isHeld = false;
            holdParent = null;

            if (canSnapAgain && targetSocket != null)
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

            transform.position = Vector3.Lerp(transform.position, worldPos, Time.deltaTime * 20f);
            transform.rotation = Quaternion.Slerp(transform.rotation, worldRot, Time.deltaTime * 14f);

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
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
                rb.useGravity = false;
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
            canSnapAgain = true;
            holdParent = null;
            transform.SetParent(null);
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
                rb.useGravity = false;
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
