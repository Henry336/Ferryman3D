using UnityEngine;

// Every gameplay number in one place, editable in the Inspector on the Game object.
public class Tuning : MonoBehaviour
{
    public static Tuning I;

    [Header("Boat tilt")]
    [Tooltip("Tilt (degrees) past which water floods in and the boat sinks.")] public float maxSafeAngle = 25f;
    [Tooltip("Seconds the boat may stay past maxSafeAngle (taking on water) before it floods.")] public float floodGrace = 0.6f;
    [Tooltip("Angular acceleration (deg/s²) per unit of torque.")] public float torqueToAccel = 4f;
    [Tooltip("Spring pulling the boat back to level (deg/s² per degree).")] public float leveling = 1.2f;
    [Tooltip("Angular velocity damping per second.")] public float angularDamping = 1.6f;
    [Tooltip("Max rotation speed (deg/s).")] public float maxAngularSpeed = 60f;
    [Tooltip("Half the deck length (units).")] public float halfLength = 3.8f;
    [Tooltip("Deck height above the boat origin.")] public float deckY = 0.2f;
    [Tooltip("Rim height above the boat origin (where hands grab).")] public float rimY = 0.55f;
    [Tooltip("How much the boat rises as it tilts, so the low rim meets the water near the limit.")] public float tiltLift = 1.6f;

    [Header("Sink meter (middle hands)")]
    [Tooltip("Sink meter fill per second per hand in the middle (1 = full).")] public float sinkFillPerHand = 0.035f;
    [Tooltip("Sink meter drain per second with no middle hands.")] public float sinkDrain = 0.08f;
    [Tooltip("How much lower the boat sits at a full sink meter.")] public float sinkDepth = 0.7f;

    [Header("Reaper")]
    [Tooltip("Reaper weight (torque = weight x offset).")] public float reaperWeight = 1f;
    [Tooltip("Top walking speed (units/s).")] public float maxSpeed = 3.6f;
    [Tooltip("Speed-up rate (units/s²).")] public float accel = 14f;
    [Tooltip("Slow-down / turn rate (units/s²).")] public float decel = 18f;
    [Tooltip("How close to the ends the Reaper can walk.")] public float edgePadding = 0.55f;
    [Tooltip("Downhill slide strength on a tilted deck.")] public float slideFactor = 0.2f;
    [Tooltip("Tilt (deg) below which the Reaper doesn't slide.")] public float slideDeadZone = 4f;

    [Header("Scythe")]
    [Tooltip("Reach in front of the Reaper for slashing hands.")] public float slashRange = 1.9f;
    [Tooltip("Hands this far behind the Reaper still count as in front.")] public float slashBehind = 0.6f;
    [Tooltip("Seconds between swings.")] public float slashCooldown = 0.35f;
    [Tooltip("Swing animation length (s).")] public float slashDuration = 0.24f;
    [Tooltip("Reach for poking a soul back to the centre.")] public float pokeRange = 1.4f;
    [Tooltip("Seconds a poked vengeful soul stays calm.")] public float pokeStun = 2.5f;
    [Tooltip("Hit-pause length on a successful slash (s, real time).")] public float hitPause = 0.05f;

    [Header("Hands")]
    [Tooltip("Pull per hand at an end, as weight.")] public float handPull = 0.9f;
    [Tooltip("Seconds for a grab's pull to build to full strength.")] public float handPullRamp = 1.5f;
    [Tooltip("Seconds for hands to rise and grab.")] public float handRiseTime = 0.5f;

    [Header("Passenger souls")]
    [Tooltip("Weight of a calm soul.")] public float calmWeight = 0.35f;
    [Tooltip("Weight of a vengeful soul.")] public float vengefulWeight = 0.55f;
    [Tooltip("Calm soul drift speed.")] public float calmSpeed = 0.7f;
    [Tooltip("Vengeful soul walk speed.")] public float vengefulSpeed = 1.1f;

    [Header("River")]
    [Tooltip("How strongly the wave slope under the boat tilts it.")] public float waveTorque = 2.2f;
    [Tooltip("Base forward speed (units/s).")] public float baseSpeed = 3f;

    void Awake() { I = this; }
}
