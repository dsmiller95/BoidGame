using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Boids.Domain.GridSnap
{
    [ExecuteAlways]
    public class GridSnapInEditor : MonoBehaviour
    {
        // TODO: get from the GridSnapSystem? maybe?
        public GridDefinition gridDefinition = GridDefinition.Default;

        public string debugName = "";
        
        private void Update()
        {
            if (!Application.isPlaying)
            {
                if (this.transform.parent != null)
                {
                    var parentSnapper = this.transform.parent.GetComponentInParent<GridSnapInEditor>();
                    if (parentSnapper != null)
                    {
                        var parentMove = parentSnapper.SnapToGrid();
                        // don't try to snap if our parent is the one that seems to be moving.
                        if (parentMove != Vector3.zero) return;
                    }
                }
                SnapToGrid();
            }
        }

        private Vector3 SnapToGrid()
        {
            Vector3 position = transform.position;
            Vector2 snappedPosition = SnapTo(new Vector2(position.x, position.y));
            var newPosition = new Vector3(snappedPosition.x, snappedPosition.y, position.z);
            var delta = position - newPosition; 
            if (delta.magnitude <= 0.0000001)
            {
                Log("zero");
                return Vector3.zero;
            }

            Log("nonzero " + delta);
            
            transform.position = new Vector3(snappedPosition.x, snappedPosition.y, position.z);
            
            return position - transform.position;
        }

        private void Log(string msg)
        {
            if (string.IsNullOrEmpty(debugName)) return;
            Debug.Log(debugName + " : " + msg, this);
        }

        private Vector2 SnapTo(Vector2 worldspace)
        {
            return gridDefinition.SnapToClosest(worldspace);
        }
    }
}