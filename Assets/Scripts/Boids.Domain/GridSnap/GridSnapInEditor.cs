using System;
using UnityEngine;

namespace Boids.Domain.GridSnap
{
    [ExecuteAlways]
    public class GridSnapInEditor : MonoBehaviour
    {
        // TODO: get from the GridSnapSystem? maybe?
        public GridDefinition gridDefinition = GridDefinition.Default;
        
        private void Update()
        {
            if (!Application.isPlaying)
            {
                SnapToGrid();
            }
        }

        private void SnapToGrid()
        {
            Vector3 position = transform.position;
            Vector2 snappedPosition = SnapTo(new Vector2(position.x, position.y));
            transform.position = new Vector3(snappedPosition.x, snappedPosition.y, position.z);
        }

        private Vector2 SnapTo(Vector2 worldspace)
        {
            return gridDefinition.SnapToClosest(worldspace);
        }
    }
}