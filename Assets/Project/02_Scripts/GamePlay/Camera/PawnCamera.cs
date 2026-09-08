using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.GamePlay
{
    /// <summary>
    /// 내 캐릭터를 따라다니는 3인칭 카메라.
    ///
    /// 철저히 로컬 전용이다. 네트워크에 올리지 않고, 프리팹에도 넣지 않는다.
    /// 남의 Pawn에 카메라가 붙으면 그 사람 화면이 내 쪽으로 끌려간다.
    ///
    /// 마우스는 카메라만 돌린다. 캐릭터의 방향은 PlayerBrain이 이동 방향에서 정한다.
    /// 캐릭터를 카메라 쪽으로 돌려버리면 옆걸음이 생기는데, AI는 옆걸음을 치지 않아서
    /// 그것만으로 사람이 구분된다. 이 게임에서는 치명적이다.
    /// </summary>
    public sealed class PawnCamera : MonoBehaviour
    {
        public static PawnCamera Instance { get; private set; }

        [Header("위치")]
        [Tooltip("캐릭터 발밑 기준으로 카메라가 바라볼 지점")]
        [SerializeField] private Vector3 _pivotOffset = new Vector3(0f, 1.4f, 0f);

        [SerializeField] private float _distance = 5f;

        [Header("회전")]
        [SerializeField] private float _sensitivity = 0.12f;
        [SerializeField] private float _minPitch = -5f;
        [SerializeField] private float _maxPitch = 60f;

        [Header("부드럽게")]
        [Tooltip("따라가는 지연. 0에 가까울수록 딱 붙는다")]
        [SerializeField] private float _followSmooth = 0.06f;

        [Header("벽 통과 방지")]
        [SerializeField] private LayerMask _obstacleMask = ~0;
        [SerializeField] private float _cameraRadius = 0.25f;

        private Transform _target;
        private float _yaw;
        private float _pitch = 20f;
        private Vector3 _followVelocity;

        /// <summary>PlayerBrain이 이동 방향을 카메라 기준으로 돌릴 때 쓴다.</summary>
        public float Yaw => _yaw;

        public bool HasTarget => _target != null;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetTarget(Transform target)
        {
            _target = target;

            if (target != null)
            {
                // 처음 붙을 때는 캐릭터가 보는 쪽에서 시작해야 화면이 튀지 않는다.
                _yaw = target.eulerAngles.y;
                SnapToTarget();
                LockCursor(true);
            }
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            HandleCursorToggle();
            ReadLook();

            Vector3 pivot = _target.position + _pivotOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desired = pivot - rotation * Vector3.forward * ResolveDistance(pivot, rotation);

            transform.position = Vector3.SmoothDamp(
                transform.position, desired, ref _followVelocity, _followSmooth);
            transform.rotation = rotation;
        }

        private void ReadLook()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();

            _yaw += delta.x * _sensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * _sensitivity, _minPitch, _maxPitch);
        }

        // 벽 뒤로 카메라가 파고들면 캐릭터가 안 보인다. 벽에 닿으면 앞으로 당긴다.
        private float ResolveDistance(Vector3 pivot, Quaternion rotation)
        {
            Vector3 direction = -(rotation * Vector3.forward);

            if (Physics.SphereCast(pivot, _cameraRadius, direction, out RaycastHit hit,
                    _distance, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                return Mathf.Max(hit.distance, 0.5f);
            }

            return _distance;
        }

        private void SnapToTarget()
        {
            Vector3 pivot = _target.position + _pivotOffset;
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            transform.position = pivot - rotation * Vector3.forward * _distance;
            transform.rotation = rotation;
            _followVelocity = Vector3.zero;
        }

        // 에디터를 두 개 띄워놓고 테스트하는데 커서가 잠겨 있으면 창을 못 바꾼다.
        // Esc로 풀고, 화면을 클릭하면 다시 잠긴다.
        private void HandleCursorToggle()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                LockCursor(false);
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                LockCursor(true);
            }
        }

        private static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
