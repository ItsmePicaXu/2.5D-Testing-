using UnityEngine;
using System.Linq;

public class InteractiveShadows : MonoBehaviour
{
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private Transform lightTransform;
    private LightType lightType;

    [SerializeField] private LayerMask targetLayerMask;
    [SerializeField] private Vector3 extrusionDirection = Vector3.zero;

    private Vector3[] objectVertices;
    private Mesh shadowColliderMesh;
    private MeshCollider shadowCollider;

    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private Vector3 previousScale;

    private bool canUpdateCollider = true;
    [SerializeField][Range(0.02f, 1f)] private float shadowColliderUpdateTime = 0.08f;

    private void Awake()
    {
        // Kiểm tra shadowTransform
        if (shadowTransform == null)
        {
            Debug.LogError("shadowTransform is not assigned in the Inspector!", this);
            return;
        }

        // Kiểm tra lightTransform
        if (lightTransform == null)
        {
            Debug.LogError("lightTransform is not assigned in the Inspector!", this);
            return;
        }

        // Tự động gán layer "Terrain" cho targetLayerMask
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        if (terrainLayer == -1)
        {
            Debug.LogError("Layer 'Terrain' does not exist! Please create it in the Layer settings.", this);
            return;
        }
        targetLayerMask = 1 << terrainLayer; // Chỉ tương tác với layer "Terrain"

        InitializeShadowCollider();

        Light light = lightTransform.GetComponent<Light>();
        if (light == null)
        {
            Debug.LogError("lightTransform does not have a Light component!", this);
            return;
        }
        lightType = light.type;

        MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter component is missing on this GameObject!", this);
            return;
        }
        objectVertices = meshFilter.mesh.vertices.Distinct().ToArray();

        shadowColliderMesh = new Mesh();
    }

    private void Update()
    {
        if (shadowTransform != null)
        {
            shadowTransform.position = transform.position;
        }
    }

    private void FixedUpdate()
    {
        if (TransformHasChanged() && canUpdateCollider)
        {
            Invoke("UpdateShadowCollider", shadowColliderUpdateTime);
            canUpdateCollider = false;
        }

        previousPosition = transform.position;
        previousRotation = transform.rotation;
        previousScale = transform.localScale;
    }

    private void InitializeShadowCollider()
    {
        GameObject shadowGameObject = shadowTransform.gameObject;
        shadowCollider = shadowGameObject.AddComponent<MeshCollider>();
        shadowCollider.convex = true;
        shadowCollider.isTrigger = true;
    }

    private void UpdateShadowCollider()
    {
        shadowColliderMesh.vertices = ComputeShadowColliderMeshVertices();
        shadowCollider.sharedMesh = shadowColliderMesh;
        canUpdateCollider = true;
    }

    private Vector3[] ComputeShadowColliderMeshVertices()
    {
        Vector3[] points = new Vector3[2 * objectVertices.Length];
        Vector3 raycastDirection = lightTransform.forward;
        int n = objectVertices.Length;

        for (int i = 0; i < n; i++)
        {
            Vector3 point = transform.TransformPoint(objectVertices[i]);

            if (lightType != LightType.Directional)
            {
                raycastDirection = point - lightTransform.position;
            }

            points[i] = ComputeIntersectionPoint(point, raycastDirection);
            points[n + i] = ComputeExtrusionPoint(point, points[i]);
        }

        return points;
    }

    private Vector3 ComputeIntersectionPoint(Vector3 fromPosition, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(fromPosition, direction, out hit, Mathf.Infinity, targetLayerMask))
        {
            return hit.point - transform.position;
        }
        return fromPosition + 100 * direction - transform.position;
    }

    private Vector3 ComputeExtrusionPoint(Vector3 objectVertexPosition, Vector3 shadowPointPosition)
    {
        if (extrusionDirection.sqrMagnitude == 0)
        {
            return objectVertexPosition - transform.position;
        }
        return shadowPointPosition + extrusionDirection;
    }

    private bool TransformHasChanged()
    {
        return previousPosition != transform.position || previousRotation != transform.rotation || previousScale != transform.localScale;
    }
}