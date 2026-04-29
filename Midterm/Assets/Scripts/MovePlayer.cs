using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//************** use UnityOSC namespace...
using UnityOSC;
//*************

public class MovePlayer : MonoBehaviour
{

    public float speed;
    public float jumpForce;
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;
    public Text countText;
    public float groundCheckDistance = 1.2f;

    private Rigidbody rb;
    private int count;
    private bool isGrounded;
    private bool jumpPressed = false;
    public LayerMask groundLayer;
    private float lastGroundSoundTime = 0f;
    private float groundSoundCooldown = 0.5f;

    //************* Need to setup this server dictionary...
    Dictionary<string, ServerLog> servers = new Dictionary<string, ServerLog>();
    //*************

    // Use this for initialization
    void Start()
    {
        Application.runInBackground = true;

        //************* Instantiate the OSC Handler...
        OSCHandler.Instance.Init();
        OSCHandler.Instance.SendMessageToClient("pd", "/unity/trigger", "ready");
        OSCHandler.Instance.SendMessageToClient("pd", "/unity/playseq", 1);
        OSCHandler.Instance.SendMessageToClient("pd2", "/unity/trigger", "ready");
        OSCHandler.Instance.SendMessageToClient("pd2", "/unity/playseq", 1);
        //*************

        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        rb.drag = 1f;
        rb.mass = 1f;

        count = 0;
        setCountText();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            jumpPressed = true;
        }
    }

    void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");
        float moveVertical = Input.GetAxis("Vertical");

        // Walking movement/Jumping
        Vector3 movement = new Vector3(moveHorizontal, 0, moveVertical);
        Vector3 newVelocity = movement * speed;

        if (jumpPressed)
        {
            newVelocity.y = jumpForce;
            jumpPressed = false;
        }
        else
        {
            newVelocity.y = rb.velocity.y;
        }

        rb.velocity = newVelocity;

        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.deltaTime;
        }

        // Ground check
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);

        //************* Routine for receiving the OSC...
        OSCHandler.Instance.UpdateLogs();
        Dictionary<string, ServerLog> servers = new Dictionary<string, ServerLog>();
        servers = OSCHandler.Instance.Servers;

        foreach (KeyValuePair<string, ServerLog> item in servers)
        {
            if (item.Value.log.Count > 0)
            {
                int lastPacketIndex = item.Value.packets.Count - 1;
                countText.text = item.Value.packets[lastPacketIndex].Address.ToString();
                countText.text += item.Value.packets[lastPacketIndex].Data[0].ToString();
            }
        }
        //*************
    }

    // When we hit the ground
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            if (Time.time - lastGroundSoundTime > groundSoundCooldown)
            {
                OSCHandler.Instance.SendMessageToClient("pd", "/unity/colwall", 1);
                OSCHandler.Instance.SendMessageToClient("pd2", "/unity/colwall", 1);
                lastGroundSoundTime = Time.time;
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isGrounded = true;
                return;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Pick Up"))
        {
            other.gameObject.SetActive(false);
            count = count + 1;
            setCountText();

            if (count < 2)
            {
                OSCHandler.Instance.SendMessageToClient("pd", "/unity/tempo", 500);
                OSCHandler.Instance.SendMessageToClient("pd2", "/unity/tempo", 500);
            }
            else if (count < 4)
            {
                OSCHandler.Instance.SendMessageToClient("pd", "/unity/tempo", 400);
                OSCHandler.Instance.SendMessageToClient("pd2", "/unity/tempo", 400);
            }
            else if (count < 6)
            {
                OSCHandler.Instance.SendMessageToClient("pd", "/unity/tempo", 300);
                OSCHandler.Instance.SendMessageToClient("pd2", "/unity/tempo", 300);

            }
            else if (count < 8)
            {
                OSCHandler.Instance.SendMessageToClient("pd", "/unity/tempo", 150);
                OSCHandler.Instance.SendMessageToClient("pd2", "/unity/tempo", 150);

            }
            else
            {
                OSCHandler.Instance.SendMessageToClient("pd", "/unity/playseq", 0);
                OSCHandler.Instance.SendMessageToClient("pd2", "/unity/playseq", 0);

            }
        }
        else if (other.gameObject.CompareTag("Wall"))
        {
            OSCHandler.Instance.SendMessageToClient("pd", "/unity/colwall", 1);
            OSCHandler.Instance.SendMessageToClient("pd2", "/unity/colwall", 1);

        }
    }

    void setCountText()
    {
        countText.text = "Count: " + count.ToString();
        OSCHandler.Instance.SendMessageToClient("pd", "/unity/trigger", count);
        OSCHandler.Instance.SendMessageToClient("pd2", "/unity/trigger", count);

    }
}
