/*
   _       _                _          _ _ _       
  (_)     | |              ( )        (_) | |      
   _  __ _| | _____    ___ |/ _ __ ___ _| | |_   _ 
  | |/ _` | |/ / _ \  / _ \  | '__/ _ \ | | | | | |
  | | (_| |   <  __/ | (_) | | | |  __/ | | | |_| |
  | |\__,_|_|\_\___|  \___/  |_|  \___|_|_|_|\__, |
 _/ |                                         __/ |
|__/                                         |___/ 

[Script by Jake O'Reilly, Jul 2024]

github(https://github.com/JakeDaSpud)
twitter(https://twitter.com/jor_gamedev)

NOTES:
-> this script requires the "new" (2019...) Unity InputSystem add-on to be installed
-> this script does NOT deal with Camera Management
-> attach this script to a 3D GameObject with a Rigidbody and Player Input Component

*/

using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Controller : MonoBehaviour
{
    // Variables
    [Header("Player Settings")]
    private Rigidbody _Player;
    [SerializeField] private float player_speed = 80.0f;
    [SerializeField] private bool look_in_new_direction = true;
    [SerializeField] private bool snap_to_new_direction = false;
    [SerializeField] private float turning_speed = 15.0f;
    private PlayerInput player_input;
    private InputAction input_move_action; // Typically WASD, Arrow Keys, Left-Stick
    private InputAction input_jump_action; // Typically Space, South Button, W, Up-Arrow Key
    private InputAction input_run_action; // Typically Left-Shift, L3, Left Bumper
    private InputAction input_crouch_action; // Typically Left-Control, R3, East Button
    private InputAction input_use_action; // Typically E, Left-Click, West Button
    private Vector3 _velocity;
    
    [Header("Jump Settings")]
    [SerializeField] private bool can_jump = true;
    [SerializeField] private Vector3 jump_force = new Vector3(0f, 2f, 0f);
    [Tooltip("Shoots straight down, decides how close the floor must be for the Player to be considered grounded.")]
    [SerializeField] private float ground_raycast_length = 1.1f;
    private bool _is_jumping = false;
    
    [Header("Run Settings")]
    [SerializeField] private bool can_run = true;
    [SerializeField] private float player_run_speed = 120.0f;
    private bool _is_running = false;
    
    [Header("Crouch Settings")]
    [SerializeField] private bool can_crouch = true;
    [SerializeField] private float crouch_speed = 8.0f;
    [SerializeField] private float crouch_scale = 0.45f;
    private bool _is_crouching = false;

    [Header("Use / Action Settings")]
    [SerializeField] private bool can_use = true;
    [Tooltip("Shoots straight ahead in Player's facing direction, decides if there's something in front of the Player.")]
    [SerializeField] private float use_raycast_length = 2.0f;



    // Awake is called when the MonoBehaviour is created
    private void Awake() {
        // Initialising Variables
        _Player = GetComponent<Rigidbody>();
        
        player_input = GetComponent<PlayerInput>();
        input_move_action = player_input.actions.FindAction("Move");
        input_jump_action = player_input.actions.FindAction("Jump");
        input_run_action = player_input.actions.FindAction("Run");
        input_crouch_action = player_input.actions.FindAction("Crouch");
        input_use_action = player_input.actions.FindAction("Use");
    }

    // Update is called once per frame
    private void Update() {
        _Handle_Input();
    }

    // FixedUpdate is called once per framerate frame
    private void FixedUpdate() {

        // Let grounded Player Jump
        if (_Is_Grounded()) {
            _is_jumping = false;
        }

        // Only do if there is input
        if (_velocity != Vector3.zero) {
            _Handle_Movement();
        }
    }

    /// <summary>
    /// Reads input actions.
    /// </summary>
    private void _Handle_Input() {
        Vector2 _move_force = new Vector2(_Player.transform.rotation.x, _Player.transform.rotation.z);
        
        if (input_use_action.WasPressedThisFrame() && can_use) {
            _Use();
        }

        if (input_crouch_action.WasPressedThisFrame() && can_crouch) {
            _Crouch();
        }

        if (input_jump_action.IsPressed() && can_jump) {
            _Jump();
        }

        if (input_run_action.IsPressed() && can_run) {
            _is_running = true;
        } else { _is_running = false; }

        // Read input move action for float values to move in
        _move_force = input_move_action.ReadValue<Vector2>();

        var _speed = player_speed;

        // Check if Player is running
        if (can_run && _is_running) {
            _speed = player_run_speed;
        }

        // Check if Player is crouching
        if (can_crouch && _is_crouching) {
            _speed = crouch_speed;
        }

        // Final Calculation before Moving the Player
        _velocity = new Vector3(_move_force.x, 0, _move_force.y).normalized * _speed * Time.deltaTime;
    }

    /// <summary>
    /// Applies Movement Force to the Player.
    /// </summary>
    private void _Handle_Movement() {
        // Only make the Player move once no matter the input
        _Player.MovePosition(_Player.transform.position + _velocity);

        // Make the Player face that direction
        if (look_in_new_direction) {
            Quaternion _target_rotation = Quaternion.LookRotation(_velocity);

            // No slerp, snaps to new direction
            if (snap_to_new_direction) {
                _Player.MoveRotation(_target_rotation);
            } 
            
            // Spherical-linear interpolation towards the new direction
            else {
                _Player.MoveRotation(Quaternion.Slerp(_Player.rotation, _target_rotation, turning_speed * Time.deltaTime));
            }
        }
    }

    /// <summary>
    /// Toggles the Player's Crouch on and off (if _can_crouch is true).
    /// </summary>
    private void _Crouch() {
        if (!can_crouch) {
            return;
        }

        // Make Player stand up
        if (_is_crouching) {
            Debug.Log("Uncrouching / Standing Up");
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y / crouch_scale, transform.localScale.z);
        } 
        
        // Make Player crouch
        else {
            Debug.Log("Crouching");
            transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * crouch_scale, transform.localScale.z);
            _Player.MovePosition(new Vector3(_Player.transform.position.x, _Player.transform.position.y - (crouch_scale - 0.1f), _Player.transform.position.z));
        }

        // Invert _is_crouching
        _is_crouching = !_is_crouching;
    }

    /// <summary>
    /// Skeleton for Use logic
    /// </summary>
    private void _Use() {
        if (!can_use) {
            Debug.Log("Can't use");
            return;
        }

        if (_There_Is_Useable_Object()) {
            Debug.Log("Using / Interacting");
            /* Do something else now! */
        }
    }

    /// <summary>
    /// Shoots a Raycast straight down to see if the Player is close to the ground.
    /// </summary>
    /// <returns>True if the Player is touching the ground.</returns>
    private bool _Is_Grounded() {
        RaycastHit _raycast_hit;
        
        if (Physics.Raycast(transform.position, Vector3.down, out _raycast_hit, ground_raycast_length)) {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Shoots a Raycast straight ahead to see if the Player can activate / use something.
    /// </summary>
    /// <returns>True if the Player is looking at something.</returns>
    private bool _There_Is_Useable_Object() {
        RaycastHit _raycast_hit;

        if (Physics.Raycast(_Player.transform.position, _Player.transform.forward, out _raycast_hit, use_raycast_length)) {
            Debug.Log("Can interact with something");
            return true;
        }

        Debug.Log("Nothing to interact with");
        return false;
    }

    /// <summary>
    /// Makes the Player Jump.
    /// </summary>
    private void _Jump() {
        // Player can't Jump while Jumping (No Double+ Jump here)
        if (!can_jump || _is_jumping) {
            Debug.Log("Can't Jump");
            return;
        }

        if (_Is_Grounded()) {
            Debug.Log("Jumping");
            _Player.AddForce(jump_force, ForceMode.Impulse);
            _is_jumping = true;
        }
    }
}
