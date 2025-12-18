using System;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

// Garantiza que el objeto tenga el componente PlayerInput
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviourPun
{
    // Evento estático para notificar a la cámara u otros sistemas que el jugador local está listo
    public static event Action<Transform> OnLocalPlayerReady;

    [Header("Ajustes de Movimiento")]
    public float speed = 5f;          // Velocidad de movimiento lineal
    public float turnSpeed = 200f;    // Velocidad de rotación en grados por segundo

    // Referencias internas
    private PlayerInput playerInput;
    private bool isMyCharacter;       // Bandera para almacenar si somos el dueño de este objeto

    /// <summary>
    /// Awake se ejecuta al instanciar el objeto, antes que Start.
    /// </summary>
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();

        // 1. DESACTIVACIÓN PREVENTIVA DE INPUT
        // Desactivamos el componente PlayerInput inmediatamente.
        // Esto es CRUCIAL para evitar el error "Cannot find matching control scheme"
        // cuando se instancian los clones de los jugadores remotos.
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }
    }

    /// <summary>
    /// Start configura la lógica de red y decide si activar el control.
    /// </summary>
    void Start()
    {
        // 2. VERIFICACIÓN DE PROPIEDAD (OWNERSHIP)
        // Guardamos en una variable si este personaje me pertenece a mí (local) o a otro jugador (remoto).
        isMyCharacter = photonView.IsMine;

        // Log de depuración para verificar identidades en consola
        Debug.Log($"[PlayerController] InstanceID: {gameObject.GetInstanceID()} - Soy mío: {isMyCharacter} - Dueño: {photonView.Owner.NickName}");

        // --- LÓGICA PARA JUGADORES REMOTOS ---
        if (!isMyCharacter)
        {
            // Si no soy el dueño, no hago nada más.
            // El input se mantiene desactivado (desde Awake) y el movimiento lo manejará PhotonTransformView.
            return;
        }

        // --- LÓGICA PARA JUGADOR LOCAL ---
        // Si soy el dueño, activo el sistema de entrada para poder controlarlo.
        if (playerInput != null)
        {
            playerInput.enabled = true;
        }

        // Avisamos al sistema (ej. Cámara) que ya existimos y puede seguirnos.
        OnLocalPlayerReady?.Invoke(transform);
    }

    void Update()
    {
        // 3. GUARDIA DE EJECUCIÓN
        // Si este script está en un personaje remoto, detenemos la ejecución aquí.
        // Solo el dueño debe procesar el input y mover su propio personaje.
        if (!isMyCharacter) return;

        // Verificamos que el input sea válido antes de leer
        if (playerInput != null && playerInput.enabled && playerInput.actions["Move"] != null)
        {
            // Leemos el valor del Joystick o WASD
            Vector2 input = playerInput.actions["Move"].ReadValue<Vector2>();

            // Si hay movimiento (magnitud mayor a un pequeño umbral para evitar drift)
            if (input.sqrMagnitude > 0.01f)
            {
                // Convertimos el input 2D (X, Y) a dirección 3D en el suelo (X, Z)
                Vector3 direction = new Vector3(input.x, 0, input.y).normalized;

                // 4. MOVIMIENTO (Translate)
                // Movemos el objeto directamente en el espacio mundial.
                // Nota: Requiere que el Rigidbody tenga alta masa/drag para ser un "tanque" ante colisiones externas.
                transform.Translate(direction * speed * Time.deltaTime, Space.World);

                // 5. ROTACIÓN
                // Calculamos hacia dónde queremos mirar
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // Rotamos suavemente hacia esa dirección
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
        }
    }
}