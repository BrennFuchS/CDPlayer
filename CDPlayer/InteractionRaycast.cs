using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace CDplayer
{
    public class InteractionRaycast : MonoBehaviour
    {
        public RaycastHit hitInfo;

        public bool hasHit = false;
        public float rayDistance = 1.35f;
        public int layerMask;

        void Start()
        {
            hitInfo = new RaycastHit();

            layerMask = LayerMask.GetMask("Dashboard");
        }

        void FixedUpdate()
        {
            if (Camera.main != null) hasHit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitInfo, rayDistance, layerMask);
        }

        public bool GetHit(Collider collider) => hasHit && hitInfo.collider == collider;
        public bool GetHitAny(Collider[] colliders) => hasHit && colliders.Any(collider => collider == hitInfo.collider);
        public bool GetHitAny(List<Collider> colliders) => hasHit && colliders.Any(collider => collider == hitInfo.collider);
    }
}