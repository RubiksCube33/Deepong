using UnityEngine;

/// <summary>
/// Manages different VR movement systems and ensures proper coordination
/// Prevents conflicts between VROriginFollower, VRInputToThirdPerson, and HybridVRMovementController
/// </summary>
public class VRMovementSystemManager : MonoBehaviour
{
    [Header("Movement System Components")]
    [SerializeField] private HybridVRMovementController hybridController;
    [SerializeField] private VROriginFollower vrOriginFollower;
    [SerializeField] private VRInputToThirdPerson vrInputToThirdPerson;

    [Header("System Selection")]
    [SerializeField] private MovementSystemType activeSystem = MovementSystemType.Hybrid;
    [SerializeField] private bool allowSystemSwitching = true;
    [SerializeField] private KeyCode systemSwitchKey = KeyCode.Tab;

    public enum MovementSystemType
    {
        BodyTrackingOnly,    // VROriginFollower only
        ControllerOnly,      // VRInputToThirdPerson only  
        Hybrid,              // HybridVRMovementController
        None                 // All systems disabled
    }

    private MovementSystemType currentActiveSystem;
    private bool isInitialized = false;

    private void Awake()
    {
        FindComponents();
        InitializeSystem();
    }

    private void Start()
    {
        SwitchToSystem(activeSystem);
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        // Handle system switching with keyboard (for testing)
        if (allowSystemSwitching && Input.GetKeyDown(systemSwitchKey))
        {
            CycleToNextSystem();
        }
    }

    private void FindComponents()
    {
        // Try to find components if not assigned
        if (hybridController == null)
            hybridController = GetComponent<HybridVRMovementController>();

        if (vrOriginFollower == null)
            vrOriginFollower = GetComponent<VROriginFollower>();

        if (vrInputToThirdPerson == null)
            vrInputToThirdPerson = GetComponent<VRInputToThirdPerson>();

        // Log found components
        Debug.Log($"[VRMovementSystemManager] Found components:");
        Debug.Log($"  - HybridController: {(hybridController != null ? "✓" : "✗")}");
        Debug.Log($"  - VROriginFollower: {(vrOriginFollower != null ? "✓" : "✗")}");
        Debug.Log($"  - VRInputToThirdPerson: {(vrInputToThirdPerson != null ? "✓" : "✗")}");
    }

    private void InitializeSystem()
    {
        // Ensure all systems start disabled
        DisableAllSystems();
    }

    public void SwitchToSystem(MovementSystemType systemType)
    {
        if (currentActiveSystem == systemType)
            return;

        Debug.Log($"[VRMovementSystemManager] Switching from {currentActiveSystem} to {systemType}");

        // Disable all systems first
        DisableAllSystems();

        // Enable the requested system
        switch (systemType)
        {
            case MovementSystemType.BodyTrackingOnly:
                EnableBodyTrackingOnly();
                break;

            case MovementSystemType.ControllerOnly:
                EnableControllerOnly();
                break;

            case MovementSystemType.Hybrid:
                EnableHybridSystem();
                break;

            case MovementSystemType.None:
                // All systems remain disabled
                break;
        }

        currentActiveSystem = systemType;
        activeSystem = systemType;
    }

    private void DisableAllSystems()
    {
        if (hybridController != null)
            hybridController.enabled = false;

        if (vrOriginFollower != null)
            vrOriginFollower.enabled = false;

        if (vrInputToThirdPerson != null)
            vrInputToThirdPerson.enabled = false;

        Debug.Log("[VRMovementSystemManager] All movement systems disabled");
    }

    private void EnableBodyTrackingOnly()
    {
        if (vrOriginFollower != null)
        {
            vrOriginFollower.enabled = true;
            Debug.Log("[VRMovementSystemManager] Body tracking only system enabled");
        }
        else
        {
            Debug.LogWarning("[VRMovementSystemManager] VROriginFollower component not found!");
        }
    }

    private void EnableControllerOnly()
    {
        if (vrInputToThirdPerson != null)
        {
            vrInputToThirdPerson.enabled = true;
            vrInputToThirdPerson.EnableVRInput(); // This disables VROriginFollower
            Debug.Log("[VRMovementSystemManager] Controller only system enabled");
        }
        else
        {
            Debug.LogWarning("[VRMovementSystemManager] VRInputToThirdPerson component not found!");
        }
    }

    private void EnableHybridSystem()
    {
        if (hybridController != null)
        {
            hybridController.enabled = true;
            Debug.Log("[VRMovementSystemManager] Hybrid movement system enabled");
        }
        else
        {
            Debug.LogWarning("[VRMovementSystemManager] HybridVRMovementController component not found!");
            // Fallback to body tracking
            EnableBodyTrackingOnly();
        }
    }

    public void CycleToNextSystem()
    {
        int nextSystemIndex = ((int)currentActiveSystem + 1) % System.Enum.GetValues(typeof(MovementSystemType)).Length;
        MovementSystemType nextSystem = (MovementSystemType)nextSystemIndex;
        
        SwitchToSystem(nextSystem);
    }

    // Public methods for external control
    public MovementSystemType GetCurrentSystem()
    {
        return currentActiveSystem;
    }

    public void SetHybridMovementMode(HybridVRMovementController.MovementMode mode)
    {
        if (hybridController != null && currentActiveSystem == MovementSystemType.Hybrid)
        {
            hybridController.SetMovementMode(mode);
        }
    }

    public void EnableBodyTracking(bool enable)
    {
        switch (currentActiveSystem)
        {
            case MovementSystemType.Hybrid:
                if (hybridController != null)
                    hybridController.EnableBodyTracking(enable);
                break;

            case MovementSystemType.BodyTrackingOnly:
                if (vrOriginFollower != null)
                    vrOriginFollower.enabled = enable;
                break;
        }
    }

    public void EnableControllerMovement(bool enable)
    {
        switch (currentActiveSystem)
        {
            case MovementSystemType.Hybrid:
                if (hybridController != null)
                    hybridController.EnableControllerMovement(enable);
                break;

            case MovementSystemType.ControllerOnly:
                if (vrInputToThirdPerson != null)
                    vrInputToThirdPerson.enabled = enable;
                break;
        }
    }

    // Status methods for debugging
    public bool IsSystemActive(MovementSystemType systemType)
    {
        return currentActiveSystem == systemType;
    }

    public string GetSystemStatus()
    {
        string status = $"Active System: {currentActiveSystem}\n";
        status += $"Components Status:\n";
        status += $"  - Hybrid: {(hybridController != null && hybridController.enabled ? "Active" : "Inactive")}\n";
        status += $"  - Body Tracking: {(vrOriginFollower != null && vrOriginFollower.enabled ? "Active" : "Inactive")}\n";
        status += $"  - Controller Input: {(vrInputToThirdPerson != null && vrInputToThirdPerson.enabled ? "Active" : "Inactive")}";
        
        return status;
    }

    // Event handlers for UI or other systems
    public System.Action<MovementSystemType> OnSystemChanged;

    private void OnValidate()
    {
        // Update system if changed in inspector during runtime
        if (Application.isPlaying && isInitialized && activeSystem != currentActiveSystem)
        {
            SwitchToSystem(activeSystem);
        }
    }
} 