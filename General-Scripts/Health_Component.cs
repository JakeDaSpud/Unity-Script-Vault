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
-> this script attaches to anything that should have health (enemies, obstacles like crates, etc.)
-> when health reaches min_health, this object will use _Die()

*/

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.EditorTools;
using UnityEditor.UI;
using UnityEngine;

public class Health_Component : MonoBehaviour
{
    enum Destroy_option {
        none,
        destroy_on_death,
        delayed_destroy_on_death,
        disable_on_death,
        delayed_disable_on_death
    };

    // Variables
    [Header("Initial Health Settings")]
    [SerializeField] private float max_health = 100.0f;
    [Tooltip("The amount of health where this object will die.")]
    [SerializeField] private float min_health = 0.0f;
    [Tooltip("None: Do not destroy or disable the gameObject on death.\nDestroy on Death: Destroy the gameObject it is attached to on death.\nDelayed Destroy on Death: Same as above, after die_delay seconds.\nDisable on Death: Disable the gameObject it is attached to on death.\nDelayed Disable on Death:  Same as above, after die_delay seconds.")]
    [SerializeField] private Destroy_option destroy_option = Destroy_option.none;
    [Tooltip("Only works if using a delayed destroy_option.\nHow long (in seconds) until this object is destroyed after dying.")]
    [SerializeField] private float die_delay = 5.0f;

    [Header("Health Settings")]
    [SerializeField] private float current_health = 100.0f;
    [SerializeField] private bool can_take_damage = true;
    [SerializeField] private bool can_die = true;
    [SerializeField] private bool can_be_destroyed = true;

    [Header("Health Recovery Settings")]
    [SerializeField] private bool can_recover_health = false;
    [Tooltip("How much health this object should recover.")]
    [SerializeField] private float health_recovery_amount = 5.0f;
    [Tooltip("How long (in seconds) until this recovers health_recovery_amount.")]
    [SerializeField] private float health_recovery_interval = 3.0f;
    [Tooltip("Whether this object has to not get hit for a certain amount of time before starting to recover health.")]
    [SerializeField] private bool cant_take_damage_before_healing = true;
    [Tooltip("Only works if cant_take_damage_before_healing is true.\nHow long (in seconds) this object can't take damage before starting to recover health.")]
    [SerializeField] private float no_hit_time_before_healing = 5.0f;
    [Tooltip("Whether this object can receive more health than its max_health.")]
    [SerializeField] private bool can_self_overheal = false;
    [Tooltip("Whether this object can receive more health than its max_health from other entities.\nUseful for mechanics like healing spells.")]
    [SerializeField] private bool can_overheal_externally = false;
    private bool _should_die = false;
    private float _next_recover_health_time;
    private bool _should_disable = false;
    private float _should_disable_time;

    // Awake is called when the MonoBehaviour is created
    private void Awake() {
        // Set times now, so when the scene starts, this object can recover health
        _next_recover_health_time = Time.time + health_recovery_interval;
    }

    // Start is called before the first frame update
    private void Start() {}

    // FixedUpdate is called once per framerate frame
    private void FixedUpdate()
    {
        if (_should_die && can_die) {
            _Die();
        }
    }

    private void Update() {

        // _should_disable is invoked in _Die(), it should be first
        // Set this object to be disabled
        if (_should_disable && _should_disable_time <= Time.time) {
            this.gameObject.SetActive(false);
        }

        // Queue object to use _Die() on next FixedUpdate() frame
        if (current_health <= min_health && can_die) {
            _should_die = true;
        }

        // Recover health
        if (_next_recover_health_time <= Time.time && can_recover_health) {
            Recover_Health(health_recovery_amount, false);
        }

    }

    private void _Set_Next_Recover_Health_Time() {
        _next_recover_health_time = Time.time + health_recovery_interval;
    }

    private void _Set_Should_Disable_Time() {
        // The order prevents this from updating every frame
        if (!_should_disable) {
            _should_disable_time = Time.time + die_delay;
        }
    }

    /// <summary>
    /// Gains an incoming amount for this object's current_health.
    /// Public so other entities can give this object health.
    /// </summary>
    /// <param name="health_recovered">How much health this object receives.</param>
    /// <param name="called_externally">Whether this function was invoked by another entity.</param>
    public void Recover_Health(float health_recovered, bool called_externally = true) {
        current_health += health_recovered;

        // Prevent self overhealing
        if (!called_externally && !can_self_overheal && current_health > max_health) {
            current_health = max_health;
        }

        // Prevent external overhealing
        if (called_externally && !can_overheal_externally && current_health > max_health) {
            current_health = max_health;
        }

        _Set_Next_Recover_Health_Time();
    }

    /// <summary>
    /// Takes away an incoming amount from this object's current_health.
    /// Public so other entities can take away health from this object.
    /// </summary>
    /// <param name="receiving_damage">How much damage this object receives.</param>
    public void Take_Damage(float receiving_damage) {
        this.current_health -= receiving_damage;

        // Start countdown for being able to start healing again
        if (cant_take_damage_before_healing) {
            _next_recover_health_time = Time.time + no_hit_time_before_healing + health_recovery_interval;
        }
    }

    /// <summary>
    /// According to this object's destroy_option, on death, either: 
    /// -> do nothing
    /// -> destroy itself
    /// -> disable itself
    /// </summary>
    private void _Die() {
        if (destroy_option == Destroy_option.none) {
            return;
        }

        else if (destroy_option == Destroy_option.destroy_on_death) {
            Destroy(this.gameObject);
        }

        else if (destroy_option == Destroy_option.delayed_destroy_on_death) {
            Destroy(this.gameObject, die_delay);
        }

        else if (destroy_option == Destroy_option.disable_on_death) {
            this.gameObject.SetActive(false);
        }

        else if (destroy_option == Destroy_option.delayed_disable_on_death) {
            _Set_Should_Disable_Time();
            _should_disable = true;
        }

        else {
            print("How did you get here? Error with the destroy_option enum.");
        }
    }
}
