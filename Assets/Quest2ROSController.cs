using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using System.Collections;

public class Quest2ROSController : MonoBehaviour
{
    [SerializeField] InputActionReference rightPosition;
    [SerializeField] InputActionReference rightRotation;
    [SerializeField] InputActionReference leftPosition;
    [SerializeField] InputActionReference leftRotation;
    [SerializeField] InputActionReference rightGrip;
    [SerializeField] InputActionReference rightTrigger;
    [SerializeField] InputActionReference leftGrip;
    [SerializeField] InputActionReference leftTrigger;

    ROSConnection ros;
    float publishInterval = 0.05f;
    float timer;
    float logTimer;
    bool ready;

    void OnEnable()
    {
        rightPosition?.action?.Enable();
        rightRotation?.action?.Enable();
        leftPosition?.action?.Enable();
        leftRotation?.action?.Enable();
        rightGrip?.action?.Enable();
        rightTrigger?.action?.Enable();
        leftGrip?.action?.Enable();
        leftTrigger?.action?.Enable();
    }

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<PoseStampedMsg>("/q2r_right_hand_pose");
        ros.RegisterPublisher<PoseStampedMsg>("/q2r_left_hand_pose");
        ros.RegisterPublisher<BoolMsg>("/q2r_right_grip_pressed");
        ros.RegisterPublisher<RosMessageTypes.Std.Float32Msg>("/q2r_right_trigger_value");
        ros.RegisterPublisher<BoolMsg>("/q2r_left_grip_pressed");
        ros.RegisterPublisher<RosMessageTypes.Std.Float32Msg>("/q2r_left_trigger_value");
        StartCoroutine(DelayedStart());
    }

    IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(2f);
        ready = true;
        Debug.Log("[Quest2ROS] Ready to publish");
    }

    void Update()
    {
        if (!ready) return;

        timer += Time.deltaTime;
        logTimer += Time.deltaTime;
        if (timer < publishInterval) return;
        timer = 0;

        var rp = rightPosition?.action?.ReadValue<Vector3>() ?? Vector3.zero;
        var rr = rightRotation?.action?.ReadValue<Quaternion>() ?? Quaternion.identity;
        var lp = leftPosition?.action?.ReadValue<Vector3>() ?? Vector3.zero;
        var lr = leftRotation?.action?.ReadValue<Quaternion>() ?? Quaternion.identity;

        if (logTimer > 2f)
        {
            logTimer = 0;
            Debug.Log($"[Quest2ROS] R={rp} L={lp}");
        }

        // Deadman switch: publish grip button state
        float gripValue = rightGrip?.action?.ReadValue<float>() ?? 0f;
        bool gripPressed = gripValue > 0.5f;
        ros.Publish("/q2r_right_grip_pressed", new BoolMsg(gripPressed));

        // Trigger value for gripper control (0.0 = open, 1.0 = closed)
        float triggerValue = rightTrigger?.action?.ReadValue<float>() ?? 0f;
        ros.Publish("/q2r_right_trigger_value", new RosMessageTypes.Std.Float32Msg(triggerValue));

        // Left grip
        float leftGripValue = leftGrip?.action?.ReadValue<float>() ?? 0f;
        bool leftGripPressed = leftGripValue > 0.5f;
        ros.Publish("/q2r_left_grip_pressed", new BoolMsg(leftGripPressed));

        // Left trigger
        float leftTriggerValue = leftTrigger?.action?.ReadValue<float>() ?? 0f;
        ros.Publish("/q2r_left_trigger_value", new RosMessageTypes.Std.Float32Msg(leftTriggerValue));

        if (rp != Vector3.zero || rr != Quaternion.identity)
        {
            var msg = new PoseStampedMsg
            {
                header = new RosMessageTypes.Std.HeaderMsg { frame_id = "world" },
                pose = new PoseMsg
                {
                    position = rp.To<FLU>(),
                    orientation = rr.To<FLU>()
                }
            };
            ros.Publish("/q2r_right_hand_pose", msg);
        }

        if (lp != Vector3.zero || lr != Quaternion.identity)
        {
            var msg = new PoseStampedMsg
            {
                header = new RosMessageTypes.Std.HeaderMsg { frame_id = "world" },
                pose = new PoseMsg
                {
                    position = lp.To<FLU>(),
                    orientation = lr.To<FLU>()
                }
            };
            ros.Publish("/q2r_left_hand_pose", msg);
        }
    }
}
