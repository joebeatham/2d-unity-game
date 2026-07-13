using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour
{
    public float LadderLength = 5f; // How tall the ladder is (gonna be calculated ingame anyway)
    public Transform TopPoint; // GameObject for ladder top points
    public Transform BottomPoint; // GameObject for ladder bottom points
    public Transform TopJumpPoint; // Jump point to jump to after climbing up
    public Transform BottomJumpPoint; // Jump point to jump to after climbing down
    public List<Ladder> TopConnectedLadders = new List<Ladder>(); // Ladders connected to the TOP of this ladder
    public List<Ladder> BottomConnectedLadders = new List<Ladder>(); // Ladders connected to the BOTTOM of this ladder
    public bool ShowGizmos = true;

    // Start is called before the first frame update
    void Start()
    {
        // Auto-calculate ladder length
        if (TopPoint != null && BottomPoint != null)
        {
            LadderLength = Vector2.Distance(TopPoint.position, BottomPoint.position);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }

    // Find top point of ladder
    public Vector3 FindTopPoint()
    {
        if (TopPoint != null)
            return TopPoint.position;
        else
            return transform.position + Vector3.up * LadderLength;
    }

    // Find bottom point of ladder
    public Vector3 FindBottomPoint()
    {
        if (BottomPoint != null)
            return BottomPoint.position;
        else
            return transform.position;
    }

    // Find jump point after climbing to ladder top
    public Vector3 FindTopJumpPoint()
    {
        if (TopJumpPoint != null)
            return TopJumpPoint.position;
        else
            return FindTopPoint() + Vector3.right * 2f; // Fallback
    }

    // Find jump point after descending
    public Vector3 FindBottomJumpPoint()
    {
        if (BottomJumpPoint != null)
            return BottomJumpPoint.position;
        else
            return FindBottomPoint() + Vector3.right * 2f; // Fallback
    }

    // Check if this top of ladder is connected to another ladder
    public bool TopConnected(Ladder otherLadder)
    {
        return TopConnectedLadders.Contains(otherLadder);
    }

    // Check if this bottom of ladder is connected to another ladder
    public bool BottomConnected(Ladder otherLadder)
    {
        return BottomConnectedLadders.Contains(otherLadder);
    }

    // List of ladders connected to ladder top
    public List<Ladder> TopConnections()
    {
        return TopConnectedLadders;
    }

    // List og ladders connected to ladder bottom
    public List<Ladder> BottomConnections()
    {
        return BottomConnectedLadders;
    }

    // Get endpoint of top point of connected ladder
    public Vector3 TopConnectionPoint(Ladder connectedLadder)
    {
        if (!TopConnected(connectedLadder)) return Vector3.zero;
        
        Vector3 MyTopPoint = FindTopPoint();
        Vector3 TheirTopPoint = connectedLadder.FindTopPoint();
        Vector3 TheirBottomPoint = connectedLadder.FindBottomPoint();
        
        // Return whichever endpoint of other ladder is closer to this ladder's top
        return (Vector3.Distance(MyTopPoint, TheirTopPoint) < Vector3.Distance(MyTopPoint, TheirBottomPoint)) ? TheirTopPoint : TheirBottomPoint;
    }
    
    // Get endpoint of bottom point of connected ladder
    public Vector3 BottomConnectionPoint(Ladder connectedLadder)
    {
        if (!BottomConnected(connectedLadder)) return Vector3.zero;
        
        Vector3 MyBottomPoint = FindBottomPoint();
        Vector3 TheirTopPoint = connectedLadder.FindTopPoint();
        Vector3 TheirBottomPoint = connectedLadder.FindBottomPoint();
        
        // Return whichever endpoint of other ladder is closer to this ladder's bottom
        return (Vector3.Distance(MyBottomPoint, TheirTopPoint) < Vector3.Distance(MyBottomPoint, TheirBottomPoint)) ? TheirTopPoint : TheirBottomPoint;
    }

    // Draw visual connections for project report
    void OnDrawGizmos()
    {
        if (!ShowGizmos) return;
        // draw ladder line
        Vector3 BottomPoint = FindBottomPoint();
        Vector3 TopPoint = FindTopPoint();
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(BottomPoint, TopPoint);
        
        // Draw ladder steps
        for (float i = 0; i <= 1; i += 0.2f)
        {
            Vector3 StepPosition = Vector3.Lerp(BottomPoint, TopPoint, i);
            Vector3 StepLeft = StepPosition + Vector3.left * 0.3f;
            Vector3 StepRight = StepPosition + Vector3.right * 0.3f;
            Gizmos.DrawLine(StepLeft, StepRight);
        }
        
        // Draw top and bottom points
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(BottomPoint, 0.5f);
        Gizmos.DrawWireSphere(TopPoint, 0.5f);
        
        // Draw jump targets
        if (TopJumpPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(TopJumpPoint.position, 0.3f);
            Gizmos.DrawLine(TopPoint, TopJumpPoint.position);
        }
        if (BottomJumpPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(BottomJumpPoint.position, 0.3f);
            Gizmos.DrawLine(BottomPoint, BottomJumpPoint.position);
        }
        
        // Draw top connections
        Gizmos.color = Color.red; 
        foreach (Ladder connectedLadder in TopConnectedLadders)
        {
            if (connectedLadder != null)
            {
                Vector3 ConnectedTopPoint = connectedLadder.FindTopPoint();
                Vector3 ConnectedBottomPoint = connectedLadder.FindBottomPoint();
                
                float DistanceToConnectedTop = Vector3.Distance(TopPoint, ConnectedTopPoint);
                float DistanceToConnectedBottom = Vector3.Distance(TopPoint, ConnectedBottomPoint);
                
                Vector3 ConnectionTargetPoint = (DistanceToConnectedTop < DistanceToConnectedBottom) ? ConnectedTopPoint : ConnectedBottomPoint;
                string TargetTopOrBottom = (DistanceToConnectedTop < DistanceToConnectedBottom) ? "TOP" : "BOTTOM";
                
                Gizmos.DrawLine(TopPoint, ConnectionTargetPoint);
                
                Vector3 Direction = (ConnectionTargetPoint - TopPoint).normalized;
                Vector3 ArrowPosition = Vector3.Lerp(TopPoint, ConnectionTargetPoint, 0.7f);
                Gizmos.DrawLine(ArrowPosition, ArrowPosition - Direction * 0.5f + Vector3.up * 0.2f);
                Gizmos.DrawLine(ArrowPosition, ArrowPosition - Direction * 0.5f - Vector3.up * 0.2f);
                
                Gizmos.DrawLine(ArrowPosition + Vector3.left * 0.2f, ArrowPosition + Vector3.right * 0.2f);
                
                Gizmos.DrawWireSphere(ConnectionTargetPoint, 0.15f);
            }
        }
        
        // Draw bottom connections
        Gizmos.color = Color.blue; 
        foreach (Ladder connectedLadder in BottomConnectedLadders)
        {
            if (connectedLadder != null)
            {
                Vector3 ConnectedTopPoint = connectedLadder.FindTopPoint();
                Vector3 ConnectedBottomPoint = connectedLadder.FindBottomPoint();
                
                float DistanceToConnectedTop = Vector3.Distance(BottomPoint, ConnectedTopPoint);
                float DistanceToConnectedBottom = Vector3.Distance(BottomPoint, ConnectedBottomPoint);
                
                Vector3 ConnectionTargetPoint = (DistanceToConnectedTop < DistanceToConnectedBottom) ? ConnectedTopPoint : ConnectedBottomPoint;
                string TargetTopOrBottom = (DistanceToConnectedTop < DistanceToConnectedBottom) ? "TOP" : "BOTTOM";
                
                Gizmos.DrawLine(BottomPoint, ConnectionTargetPoint);
                
                Vector3 Direction = (ConnectionTargetPoint - BottomPoint).normalized;
                Vector3 ArrowPosition = Vector3.Lerp(BottomPoint, ConnectionTargetPoint, 0.7f);
                Gizmos.DrawLine(ArrowPosition, ArrowPosition - Direction * 0.5f + Vector3.up * 0.2f);
                Gizmos.DrawLine(ArrowPosition, ArrowPosition - Direction * 0.5f - Vector3.up * 0.2f);
                
                Gizmos.DrawWireCube(ArrowPosition, Vector3.one * 0.3f);
                
                Gizmos.DrawWireSphere(ConnectionTargetPoint, 0.15f);
            }
        }
    }
}
