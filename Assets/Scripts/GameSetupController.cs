using System.Collections;
using Photon.Pun;
using UnityEngine;

// Heredamos de MonoBehaviourPunCallbacks para tener acceso a eventos como OnJoinedRoom
public class GameSetupController : MonoBehaviourPunCallbacks
{
    [Header("Configuración del Jugador")]
    [Tooltip("El nombre exacto del Prefab en la carpeta Resources.")]
    [SerializeField] private string playerPrefabName = "Player";

    [Header("Configuración de Spawn")]
    [Tooltip("Radio en X y Z donde aparecerán los jugadores aleatoriamente.")]
    [SerializeField] private float spawnRadius = 5f;

    // Bandera de control para evitar que el jugador se instancie dos veces
    // (Puede pasar si Start y OnJoinedRoom ocurren muy seguido).
    private bool hasSpawned = false;

    private void Start()
    {
        // VERIFICACIÓN DE ESTADO DE CONEXIÓN:
        // Hay dos escenarios posibles al cargar esta escena:

        // ESCENARIO A: Venimos desde el Menú Principal.
        // Ya estamos conectados y en una sala. PhotonNetwork.IsConnectedAndReady será true.
        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[GameSetup] Conexión lista. Iniciando Spawn...");
            SpawnPlayer();
            StartCoroutine(PingLoop());
        }
        else
        {
            // ESCENARIO B: Dimos "Play" directamente en esta escena (Testing) o hubo un retraso.
            // No hacemos nada y esperamos a que el evento 'OnJoinedRoom' se dispare automáticamente.
            Debug.LogWarning("[GameSetup] Esperando conexión a la sala...");
        }
    }

    /// <summary>
    /// Este evento se llama automáticamente cuando el cliente termina de entrar a la sala.
    /// Es nuestra red de seguridad por si en Start() aún no estábamos listos.
    /// </summary>
    public override void OnJoinedRoom()
    {
        Debug.Log("[GameSetup] OnJoinedRoom disparado. Intentando Spawn...");
        SpawnPlayer();
        StartCoroutine(PingLoop());
    }

    private void SpawnPlayer()
    {
        // 1. GUARDIA DE SEGURIDAD
        // Si ya instanciamos al jugador, abortamos para no tener duplicados.
        if (hasSpawned) return;

        // 2. CÁLCULO DE POSICIÓN
        // Usamos Random.insideUnitCircle para una distribución circular plana (X, Z).
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = new Vector3(randomCircle.x, 1f, randomCircle.y);

        // 3. INSTANCIACIÓN EN RED
        // PhotonNetwork.Instantiate es obligatorio aquí.
        // A diferencia de Instantiate normal, esto avisa a todos los otros clientes:
        // "Oigan, acabo de crear mi personaje, créenlo en sus pantallas también".
        // NOTA: El prefab 'playerPrefabName' DEBE estar dentro de una carpeta llamada "Resources".
        PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);

        // Marcamos que ya nacimos
        hasSpawned = true;
        Debug.Log($"[GameSetup] Jugador instanciado en: {spawnPosition}");
    }

    /// <summary>
    /// Corrutina para monitorear la latencia (Ping) sin saturar el Update().
    /// </summary>
    private IEnumerator PingLoop()
    {
        // Ejecutar mientras estemos conectados
        while (PhotonNetwork.IsConnected)
        {
            int ping = PhotonNetwork.GetPing();

            // Logueamos solo si el ping es preocupante o como info periódica
            // (Aquí lo dejo simple como pediste)
            Debug.Log($"[Network] Ping: {ping} ms");

            yield return new WaitForSeconds(3f); // Comprobamos cada 3 segundos
        }
    }
}