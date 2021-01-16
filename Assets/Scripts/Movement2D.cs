using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Attach this script a RigidBody2D to the player GameObject
//Set Body type to Dynamic, Collision detection to continuous and Freeze Z rotation
//Add a Collider (Any will do)
//Define the ground and wall mask layers
//Adjust the ground raycast length and ground raycast offset (only change the X values) variables for ground check (Activate Gizmos to see)
//Adjust the top raycast length (extend the length pretty far for early corner detection), edge raycast offset and inner raycast offset variables (only change the X values) for jump corner correction check (Activate Gizmos to see)

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
    [SerializeField] private float _hangTime = .1f;
    [SerializeField] private float _jumpBufferLength = .1f;
    private int _extraJumpsValue;
    private float _hangTimeCounter;
    private float _jumpBufferCounter;
    private bool _canJump => _jumpBufferCounter > 0f && (_hangTimeCounter > 0f || _extraJumpsValue > 0);

    [Header("Ground Collision Variables")]
    [SerializeField] private float _groundRaycastLength;
    [SerializeField] private Vector3 _groundRaycastOffset;
    private bool _onGround;

    [Header("Corner Correction Variables")]
    [SerializeField] private float _topRaycastLength;
    [SerializeField] private Vector3 _edgeRaycastOffset;
    [SerializeField] private Vector3 _innerRaycastOffset;
    private bool _canCornerCorrect;

    private void Start() {
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
    }

    private void Update()
    {
        _horizontalDirection = GetInput().x;
        if (Input.GetButtonDown("Jump"))
        {
            _jumpBufferCounter = _jumpBufferLength;
        }
        else
        {
            _jumpBufferCounter -= Time.deltaTime;
        }
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
            _hangTimeCounter = _hangTime;

            //Animation
            _anim.SetBool("isJumping", false);
            _anim.SetBool("isFalling", false);
        }
        else
        {
            ApplyAirLinearDrag();
            FallMultiplier();
            _hangTimeCounter -= Time.deltaTime;
        }
        if (_canCornerCorrect) CornerCorrect(_rb.velocity.y);
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
        _hangTimeCounter = 0f;
        _jumpBufferCounter = 0f;

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

    void CornerCorrect(float Yvelocity)
    {
        //Push player to the right
        RaycastHit2D _hit = Physics2D.Raycast(transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength,Vector3.left, _topRaycastLength, _groundLayer);
        if (_hit.collider != null)
        {
            float _newPos = Vector3.Distance(new Vector3(_hit.point.x, transform.position.y, 0f) + Vector3.up * _topRaycastLength,
                transform.position - _edgeRaycastOffset + Vector3.up * _topRaycastLength);
            transform.position = new Vector3(transform.position.x + _newPos, transform.position.y, transform.position.z);
            _rb.velocity = new Vector2(_rb.velocity.x, Yvelocity);
            return;
        }

        //Push player to the left
        _hit = Physics2D.Raycast(transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength, Vector3.right, _topRaycastLength, _groundLayer);
        if (_hit.collider != null)
        {
            float _newPos = Vector3.Distance(new Vector3(_hit.point.x, transform.position.y, 0f) + Vector3.up * _topRaycastLength,
                transform.position + _edgeRaycastOffset + Vector3.up * _topRaycastLength);
            transform.position = new Vector3(transform.position.x - _newPos, transform.position.y, transform.position.z);
            _rb.velocity = new Vector2(_rb.velocity.x, Yvelocity);
        }
    }

    private void CheckCollisions()
    {
        //Ground Collisions
        _onGround = Physics2D.Raycast(transform.position + _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer) ||
                    Physics2D.Raycast(transform.position - _groundRaycastOffset, Vector2.down, _groundRaycastLength, _groundLayer);

        //Corner Collisions
        _canCornerCorrect = Physics2D.Raycast(transform.position + _edgeRaycastOffset, Vector2.up, _topRaycastLength, _groundLayer) &&
                            !Physics2D.Raycast(transform.position + _innerRaycastOffset, Vector2.up, _topRaycastLength, _groundLayer) ||
                            Physics2D.Raycast(transform.position - _edgeRaycastOffset, Vector2.up, _topRaycastLength, _groundLayer) &&
                            !Physics2D.Raycast(transform.position - _innerRaycastOffset, Vector2.up, _topRaycastLength, _groundLayer);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        //Ground Check
        Gizmos.DrawLine(transform.position + _groundRaycastOffset, transform.position + _groundRaycastOffset + Vector3.down * _groundRaycastLength);
        Gizmos.DrawLine(transform.position - _groundRaycastOffset, transform.position - _groundRaycastOffset + Vector3.down * _groundRaycastLength);

        //Corner Check
        Gizmos.DrawLine(transform.position + _edgeRaycastOffset, transform.position + _edgeRaycastOffset + Vector3.up * _topRaycastLength);
        Gizmos.DrawLine(transform.position - _edgeRaycastOffset, transform.position - _edgeRaycastOffset + Vector3.up * _topRaycastLength);
        Gizmos.DrawLine(transform.position + _innerRaycastOffset, transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength);
        Gizmos.DrawLine(transform.position - _innerRaycastOffset, transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength);

        //Corner Distance Check
        Gizmos.DrawLine(transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength,
                        transform.position - _innerRaycastOffset + Vector3.up * _topRaycastLength + Vector3.left * _topRaycastLength);
        Gizmos.DrawLine(transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength,
                        transform.position + _innerRaycastOffset + Vector3.up * _topRaycastLength + Vector3.right * _topRaycastLength);
    }
}