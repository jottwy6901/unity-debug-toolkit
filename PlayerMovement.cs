using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    #region VARIABLES

    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float jumpForce = 5f;

    private Rigidbody rb;
    private bool isGrounded;

    #endregion

    #region UNITY METHODS

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
    }

    #endregion

    #region CORE LOGIC

    private void HandleMovement()
    {
        float h = 0f;

        if (Keyboard.current.aKey.isPressed) h = -1f;
        if (Keyboard.current.dKey.isPressed) h = 1f;

        Vector3 move = new Vector3(h * speed, rb.linearVelocity.y, 0);
        rb.linearVelocity = move;

        // LOG DE DEBUG
        if (DebugActionRunner.IsMonitored(this, "Movement"))
        {
            Debug.Log($"<color=cyan>[PlayerMovement] → Movement | h={h} speed={speed}</color>");
        }
    }

    private void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            // LOG DE DEBUG
            if (DebugActionRunner.IsMonitored(this, "Jump"))
            {
                Debug.Log($"<color=cyan>[PlayerMovement] → Jump | isGrounded={isGrounded} jumpForce={jumpForce}</color>");
            }
        }
    }

    #endregion

    #region COLLISIONS

    private void OnCollisionStay()
    {
        isGrounded = true;
    }

    private void OnCollisionExit()
    {
        isGrounded = false;
    }

    #endregion
}