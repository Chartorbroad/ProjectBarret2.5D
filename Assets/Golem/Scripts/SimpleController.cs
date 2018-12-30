using UnityEngine;
using System.Collections.Generic;

public class SimpleController : MonoBehaviour {

    private enum ControlMode
    {
        Tank,
        Direct
    }

    public Transform targetTransform;
    public Transform platformTransform;

    public int playernumber;

    [SerializeField] private float m_moveSpeed = 2;
    [SerializeField] private float m_turnSpeed = 200;
    [SerializeField] private float m_jumpForce = 4;
    [SerializeField] private Animator m_animator;
    [SerializeField] private Rigidbody m_rigidBody;

    [SerializeField] private ControlMode m_controlMode = ControlMode.Direct;

    public float horizKickScale = 1.0f;
    public float vertKickScale = 1.0f;
    public float proximityAlert = 3.0f;

    private float m_currentV = 0;
    private float m_currentH = 0;

    private readonly float m_interpolation = 10;
    private readonly float m_walkScale = 0.33f;
    private readonly float m_backwardsWalkScale = 0.16f;
    private readonly float m_backwardRunScale = 0.66f;

    private bool m_wasGrounded;
    private Vector3 m_currentDirection = Vector3.zero;

    private float m_jumpTimeStamp = 0;
    private float m_minJumpInterval = 0.25f;

    private bool m_isGrounded;
    private List<Collider> m_collisions = new List<Collider>();

    private bool m_hasLost = false;
    private bool m_hasWon = false;

    private void OnCollisionEnter(Collision collision)
    {
        ContactPoint[] contactPoints = collision.contacts;
        for(int i = 0; i < contactPoints.Length; i++)
        {
            if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
            {
                if (!m_collisions.Contains(collision.collider)) {
                    m_collisions.Add(collision.collider);
                }
                m_isGrounded = true;
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        ContactPoint[] contactPoints = collision.contacts;
        bool validSurfaceNormal = false;
        for (int i = 0; i < contactPoints.Length; i++)
        {
            if (Vector3.Dot(contactPoints[i].normal, Vector3.up) > 0.5f)
            {
                validSurfaceNormal = true; break;
            }
        }

        if(validSurfaceNormal)
        {
            m_isGrounded = true;
            if (!m_collisions.Contains(collision.collider))
            {
                m_collisions.Add(collision.collider);
            }
        } else
        {
            if (m_collisions.Contains(collision.collider))
            {
                m_collisions.Remove(collision.collider);
            }
            if (m_collisions.Count == 0) { m_isGrounded = false; }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if(m_collisions.Contains(collision.collider))
        {
            m_collisions.Remove(collision.collider);
        }
        if (m_collisions.Count == 0) { m_isGrounded = false; }
    }

	void Update () {
        //m_animator.SetBool("Grounded", m_isGrounded);

        if (!m_hasLost && !(m_hasWon))
        {
            switch (m_controlMode)
            {
                case ControlMode.Direct:
                    DirectUpdate();
                    break;

                case ControlMode.Tank:
                    TankUpdate();
                    break;

                default:
                    Debug.LogError("Unsupported state");
                    break;
            }
        }
        m_wasGrounded = m_isGrounded;

        if (fallCheck()) { lose(); }
    }

    private void kickOffPlatform(int axis)
    {
        if (axis == 0)
        {
            if (transform.position.x < 0.0)
            {
                m_rigidBody.AddForce(((Vector3.left * horizKickScale) + (Vector3.up * vertKickScale)) * m_jumpForce / 16.0f, ForceMode.Impulse);
            }
            else
            {
                m_rigidBody.AddForce(((Vector3.right * horizKickScale) + (Vector3.up * vertKickScale)) * m_jumpForce / 16.0f, ForceMode.Impulse);
            }
        }

        if (axis == 1)
        {
            if (transform.position.z < 0.0)
            {
                m_rigidBody.AddForce(((Vector3.back * horizKickScale) + (Vector3.up * vertKickScale)) * m_jumpForce / 16.0f, ForceMode.Impulse);
            }
            else
            {
                m_rigidBody.AddForce(((Vector3.forward * horizKickScale) + (Vector3.up * vertKickScale)) * m_jumpForce / 16.0f, ForceMode.Impulse);
            }
        }
    }

    private void lose()
    { 
        m_hasLost = true;
        //send message here to other golem's victory check
        targetTransform.GetComponent<SimpleController>().win();
        // need death animation
        transform.GetComponent<Animator>().SetBool("hasLost", true);
    }

    void win()
    {
        m_hasWon = true;
        // need victory animation
        Transform camera = Camera.main.transform;
        //Quaternion faceCamera = new Quaternion(transform.rotation.x, camera.rotation.y, transform.rotation.z, 1.0f);
        //transform.rotation = faceCamera;
        var fwd = Camera.main.transform.forward * -1.0f;
        //fwd.y = 0.0f;
        transform.rotation = Quaternion.LookRotation(fwd);

        transform.GetComponent<Animator>().SetBool("hasWon", true);
    }

    private bool fallCheck()
    {
        if (Mathf.Abs(transform.position.x) > (platformTransform.localScale.x / 2.0f))
        {
            kickOffPlatform(0);
            return true;
        }
        if (Mathf.Abs(transform.position.z) > (platformTransform.localScale.z / 2.0f))
        {
            kickOffPlatform(1);
            return true;
        }

        return false;

    }

    private void TankUpdate()
    {
        float v = 0;
        float h = 0;

        if (playernumber == 1)
        {
            v = Input.GetAxis("Vertical");
            h = Input.GetAxis("Horizontal");
        }
        else if (playernumber == 2)
        {
            v = Input.GetAxis("VerticalP2");
            h = Input.GetAxis("HorizontalP2");
        }
        else
        {
            Debug.Log("Player Number is invalid");
        }

        bool walk = false;// Input.GetKey(KeyCode.LeftShift);

        if (v < 0) {
            if (walk) { v *= m_backwardsWalkScale; }
            else { v *= m_backwardRunScale; }
        } else if(walk)
        {
            v *= m_walkScale;
        }

        m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
        m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

        transform.position += transform.forward * m_currentV * m_moveSpeed * Time.deltaTime;
        transform.Rotate(0, m_currentH * m_turnSpeed * Time.deltaTime, 0);

        m_animator.SetFloat("MoveSpeed", m_currentV);

        JumpingAndLanding();
    }

    private void DirectUpdate()
    {

        float v = 0;
        float h = 0;

        if (playernumber == 1)
        {
            v = Input.GetAxis("Vertical");
            h = Input.GetAxis("Horizontal");
        }
        else if (playernumber == 2)
        {
            v = Input.GetAxis("VerticalP2");
            h = Input.GetAxis("HorizontalP2");
        }
        else
        {
            Debug.Log("Player Number is invalid");
        }

        if (v == 0.0f && h == 0.0f) { m_animator.SetBool("Walking", false); }
        else { m_animator.SetBool("Walking", true); }

        Transform camera = Camera.main.transform;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            v *= m_walkScale;
            h *= m_walkScale;
        }

        m_currentV = Mathf.Lerp(m_currentV, v, Time.deltaTime * m_interpolation);
        m_currentH = Mathf.Lerp(m_currentH, h, Time.deltaTime * m_interpolation);

        Vector3 direction = camera.forward * m_currentV + camera.right * m_currentH;

        float directionLength = direction.magnitude;
        direction.y = 0;
        direction = direction.normalized * directionLength;

        if(direction != Vector3.zero)
        {
            m_currentDirection = Vector3.Slerp(m_currentDirection, direction, Time.deltaTime * m_interpolation);

            transform.rotation = Quaternion.LookRotation(m_currentDirection);
            transform.position += m_currentDirection * m_moveSpeed * Time.deltaTime;
            //m_animator.SetFloat("MoveSpeed", direction.magnitude);
            if (Vector3.Distance(targetTransform.transform.position, transform.position) < proximityAlert){
                m_animator.SetBool("Pushing", true);
            }
            else
            {
                m_animator.SetBool("Pushing", false);
            }

        }
        else
        {
            m_animator.SetBool("Walking", false);
            m_animator.SetBool("Pushing", false);
        }

        JumpingAndLanding();
    }

    private void JumpingAndLanding()
    {
        bool jumpCooldownOver = (Time.time - m_jumpTimeStamp) >= m_minJumpInterval;

        bool isJumping = false;

        if (playernumber == 1)
        {
            if (Input.GetKey(KeyCode.Space)) { isJumping = true; }
        }
        else if (playernumber == 2)
        {
            if (Input.GetKey(KeyCode.RightShift)) { isJumping = true; }
        }
        else
        {
            Debug.Log("Player Number is invalid (JumpingAndLanding)");
        }

        if (jumpCooldownOver && m_isGrounded && isJumping/*Input.GetKey(KeyCode.Space)*/)
        {
            m_jumpTimeStamp = Time.time;
            m_rigidBody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
        }

        if (!m_wasGrounded && m_isGrounded)
        {
            m_animator.SetTrigger("Land");
        }

        if (!m_isGrounded && m_wasGrounded)
        {
            m_animator.SetTrigger("Jump");
        }
    }
}
