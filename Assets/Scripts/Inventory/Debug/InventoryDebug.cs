using UnityEngine;

public class InventoryDebug : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Inventory _inventory;
    
    [Header("Test Items")]
    [SerializeField] private ItemData _testBoard;
    [SerializeField] private ItemData _testReinforcer;
    [SerializeField] private int _testQuantity = 5;

    private void Update()
    {
        if (_inventory == null) return;

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            if (_testBoard != null)
            {
                _inventory.DebugAddItem(_testBoard, _testQuantity);
                Debug.Log($"[InventoryDebug] Added {_testQuantity} boards");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            if (_testReinforcer != null)
            {
                _inventory.DebugAddItem(_testReinforcer, _testQuantity);
                Debug.Log($"[InventoryDebug] Added {_testQuantity} reinforcers");
            }
        }
    }
}
