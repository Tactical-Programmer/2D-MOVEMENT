using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Attach this script a RigidBody2D to the player GameObject
//Set Body type to Dynamic, Collision detection to continuous and Freeze Z rotation
//Add a Collider (Any will do)
//Define the ground and wall mask layers
//Adjust the ground raycast length and ground raycast offset variables for ground check (Activate Gizmos to see)

public class Movement2D : MonoBehaviour
{
    [Header("Components")]
    private Rigidbody2D _rb;
    private Animator _anim;

    [Header("Layer Masks")]
    [SerializeField] private LayerMask _groundLayer;

    [Header("Movement Variables")]
    [SerializeField] private float _movementAcceleration = 70f;
    [SerializeField] private float _maxMoveSpeed = 12f;
    [SerializeField] private float _groundLinearDrag = 7f;
    private float _horizontalDirection;
    private bool _changingDirection => (_rb.velocity.x > 0f && _horizontalDirection < 0f) || (_rb.velocity.x < 0f && _horizontalDirection > 0f);
    private bool _facingRight = true;

    [Header("Jump Variables")]
    [SerializeField] private float _jumpForce = 12f;
    [SerializeField] private float _airLinearDrag = 2.5f;
    [SerializeField] private float _fallMultiplier = 8f;
    [SerializeField] private float _lowJumpFallMultiplier = 5f;
    [SerializeField] private int _extraJumps = 1;
    private int _extraJumpsValue;
    private bool _canJump => Input.GetButtonDown("Jump") && (_onGround || _extraJumpsValue > 0);

    [Header("Ground Collision Variables")]
    [SerializeField] private float _groundRaycastLength;
    [SerializeField] private Vector3 _groundRaycastOffset;
    private bool _onGround;

    private void Start() {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        _horizontalDirection = GetInput().x;
        if (_canJump) Jump();

        //Animation
        _anim.SetBool("isGrounded", _onGround);
        _anim.SetFloat("horizontalDirection", Mathf.Abs(_horizontalDirection));
        if (_horizontalDirection < 0f && _facingRight)
        {
            Flip();
        }
        else if(_horizontalDirection > 0f && !_facingRight)
        {
            Flip();
        }
        if(_rb.velocity.y < 0f)
        { 
            _anim.SetBool("isJumping", false);
            _anim.SetBool("isFalling", true);
        }
    }

    private void FixedUpdate()
    {
        CheckCollisions();
        MoveCharacter();
        if (_onGround)
        {
            ApplyGroundLinearDrag();
            _extraJumpsValue = _extraJumps;

            //Animation
            _anim.SetBool("isJumping", false);
            _anim.SetBool("isFalling", false);
        }
        else
        {
            ApplyAirLinearDrag();
            FallMultiplier();
        }
    }

    private Vector2 GetInput()
    {
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }

    private void MoveCharacter()
    {
        _rb.AddForce(new Vector2(_horizontalDirection, 0f) * _movementAcceleration);

        if (Mathf.Abs(_rb.velocity.x) > _maxMoveSpeed)
            _rb.velocity = new Vector2(Mathf.Sign(_rb.velocity.x) * _maxMoveSpeed, _rb.velocity.y);
    }

    private void ApplyGroundLinearDrag()
    {
        if (Mathf.Abs(_horizontalDirection) < 0.4f || _changingDirection)
        {
            _rb.drag = _groundLinearDrag;
        }
        else
        {
            _rb.drag = 0f;
        }
    }

    private void ApplyAirLinearDrag()
    {
        _rb.drag = _airLinearDrag;
    }

    private void Jump()
    {
        if (!_onGround)
            _extraJumpsValue--;

        _rb.velocity = new Vector2(_rb.velocity.x, 0f);
        _rb.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);

        //Animation
        _anim.SetBool("isJumping", true);
        _anim.SetBool("isFalling", false);
    }

    private void FallMultiplier()
    {
        if (_rb.velocity.y < 0)
        {
            _rb.gravityScale = _fallMultiplier;
        }
        else if (_rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            _rb.gravityScale = _lowJumpFallMultiplier;
        }
        else
        {
            _rb.gravityScale = 1f;
        }
    }

    void Flip()
    {
        _facingRight = !_facingRight;
        transform.Rotate(0f, 180f, 0f);
    }

    private void CheckCollisions()
    {
        _onGround = Physics2D.Raycast(transform.position + _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer) ||
                    Physics2D.Raycast(transform.position - _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position + _groundRaycastOffset, transform.position + _groundRaycastOffset + Vector3.down * _groundRaycastLength);
        Gizmos.DrawLine(transform.position - _groundRaycastOffset, transform.position - _groundRaycastOffset + Vector3.down * _groundRaycastLength);
    }
}
