using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour {

    public float height; // Wysokość stawianego dowolnie obiektu
    public float rotX, rotY, rotZ; // Rotacja stawianego dowolnie obiektu

    private Transform mouseMesh; // Obiekt odpowiadający za wykrywanie miejsca kliknięcia myszki przy budowaniu dowolnym
    private Camera cam; // Kamera
    private Transform cameraContainer;

    public bool freePlacingObjects; // Stawianie dowolne

    public List<GameObject> prefabs; // Lista prefabów
    public List<Sprite> sprites; // Lista obrazów
    public List<int> cost; // Lista cen obiektów
    public List<string> prefabName; // Lista nazw obiektów
    public List<int> prefabCount; // Ilość zbudowanych obiektów

    public int prefabId; // Który element chcemy zbudować

    public bool roomGenerated; // Informacja że pomieszczenie zostało wygenerowane
    public float roomX, roomY, roomZ; // Wielkości pomieszczenia

    public Material matFloor, matWallX, matWallZ; // Materiały dla ścian i podłogi pomieszczenia

    public GameObject temp; // Obiekt który przechowuje podpowiedzi przy budowaniu

    private int layerMaskGround = 1 << 8; // Layer dla którego ma być wykrywana pozycja myszki, "mouseMesh" go używa
    private int layerMaskDelete = 1 << 9; // Layer do usuwania elementów
    private int layerMaskBuild = 1 << 10; // Layer do budowania
    private int layerMaskSpace = 1 << 11; // Layer do sprawdzania czy miejsce jest owlne.

    public GameObject kosztorys, UI_obj, kosztorys_content, UI_obj2;

    public List<GameObject> builtObjects = new List<GameObject>();

    public bool collisionDetectorOn;

    private void Awake() {
        mouseMesh = GameObject.Find("MouseMesh").transform;
        GameObject.Find("Resolution").transform.localScale = new Vector3(Screen.width / 1920.0f, Screen.height / 1080.0f, 1); // Zmiana skali UI dla danego ekranu
        cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        cameraContainer = GameObject.Find("Camera Container").transform;
        freePlacingObjects = true;
        temp = GameObject.Find("Temp");
        collisionDetectorOn = true;
    }

    private void Update() {
        if (roomGenerated == false) return;

        // Ruch kamery do góry i w dół
        if (Input.GetKey(KeyCode.Q)) cameraContainer.transform.Translate(Vector3.up * 3 * Time.deltaTime);
        if (Input.GetKey(KeyCode.E)) cameraContainer.transform.Translate(Vector3.up * 3 * -Time.deltaTime);

        mouseMesh.transform.position = new Vector3(cam.transform.position.x, height, cam.transform.position.z);

        Vector3 mousePos = Input.mousePosition;
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (EventSystem.current.IsPointerOverGameObject()) return; // Jeżeli klikneliśmy na element UI, to nie wykonuj dalszego kodu

        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z)) {
            DeleteLastElement();
        }

        if (Input.GetKey(KeyCode.LeftControl)) { // Gdy trzymamy lewy kontrol, to usuwamy elementy
            if (Physics.Raycast(ray, out hit, 100, layerMaskDelete)) {
                if (Input.GetMouseButtonDown(0)) {
                    if (hit.transform != null) {
                        if(hit.transform.parent.GetComponent<PrefabInfo>() != null) // Ponowna aktywacja BoxCollidera, który był zapisany w prefabInfo jako rodzic
                            if (hit.transform.parent.GetComponent<PrefabInfo>().parentObj != null)
                                hit.transform.parent.GetComponent<PrefabInfo>().parentObj.GetComponent<BoxCollider>().enabled = true;

                        if (hit.transform.parent.GetComponent<PrefabInfo>() != null) // Ponowna aktywacja BoxCollidera dla każdego elementu, który jest dzieckiem elementu usuwanego
                            if (hit.transform.parent.GetComponent<PrefabInfo>().childObj != null) {
                                PrefabInfo _prefabInfo = hit.transform.parent.GetComponent<PrefabInfo>();
                                for (int i = _prefabInfo.childObj.Count - 1; i >= 0; i--) {
                                    if (_prefabInfo.childObj[i] != null) {
                                        _prefabInfo.childObj[i].transform.GetChild(1).GetComponent<BoxCollider>().enabled = true;
                                    }
                                }
                            }

                        prefabCount[hit.transform.parent.GetComponent<PrefabInfo>().buildID]--;
                        Destroy(hit.transform.parent.gameObject); // Kasowanie obiektu
                        
                    }
                }
            }

            return;
        }
        mousePos = Input.mousePosition;
        hit = new RaycastHit();
        ray = cam.ScreenPointToRay(mousePos);

        if (freePlacingObjects == true) { // Jeżeli dowolne budowanie jest włączone
            if (Physics.Raycast(ray, out hit, 100, layerMaskGround)) { // Jeżeli wykryto kolizje z "mouseMesh"
                float _x = hit.point.x;
                float _z = hit.point.z;
                if (Input.GetKey(KeyCode.LeftShift)) {
                    _x = Mathf.RoundToInt(_x);
                    _z = Mathf.RoundToInt(_z);
                    if (roomX % 2 != 0) _x -= 0.5f;
                    if (roomZ % 2 != 0) _z -= 0.5f;
                }
            
                // Zmiana pozycji wspomagania budowania
                temp.transform.position = new Vector3(_x, height, _z);
                temp.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);

                

                if (Input.GetMouseButtonDown(0)) {
                    GameObject _ins = Instantiate(prefabs[prefabId], new Vector3(_x, height, _z), Quaternion.Euler(rotX, rotY, rotZ)); // Tworzenie nowego elementu

                    _ins.GetComponent<PrefabInfo>().buildID = prefabId;
                    prefabCount[prefabId]++; // Aktualizacja informacji o ilości danego elementu
                    builtObjects.Add(_ins);

                    if (CheckIfSpaceIsFreeToBuild(_ins) == false) {
                        DeleteLastElement();
                    }
                    
                }
            }
        }

        mousePos = Input.mousePosition;
        hit = new RaycastHit();
        ray = cam.ScreenPointToRay(mousePos);

        if (freePlacingObjects == false) { // Jeżeli dowolne budowanie jest wyłączone
            if (Physics.Raycast(ray, out hit, 100, layerMaskBuild)) {

                // Zmiana pozycji wspomagania budowania
                temp.transform.position = hit.transform.position;
                temp.transform.rotation = Quaternion.Euler(hit.transform.rotation.eulerAngles.x, hit.transform.rotation.eulerAngles.y, hit.transform.rotation.eulerAngles.z);
            
                if (Input.GetMouseButtonDown(0)) {
                    
                    GameObject _ins = Instantiate(prefabs[prefabId], hit.transform.position, Quaternion.Euler(hit.transform.rotation.eulerAngles.x, hit.transform.rotation.eulerAngles.y, hit.transform.rotation.eulerAngles.z)); // Tworzenie nowego elementu
                    _ins.GetComponent<PrefabInfo>().buildID = prefabId;
                    _ins.GetComponent<PrefabInfo>().parentObj = hit.transform.gameObject;
                    _ins.transform.GetChild(1).GetComponent<BoxCollider>().enabled = false; // Wyłączenie kolidera dla dziecka pierwszego (Node0) postawionego elementu
                    hit.transform.GetComponent<BoxCollider>().enabled = false; // Wyłączenie kolidera dla Node, który został kliknięty
                    hit.transform.parent.GetComponent<PrefabInfo>().childObj.Add(_ins);
                    prefabCount[prefabId]++; // Aktualizacja informacji o ilości danego elementu
                    builtObjects.Add(_ins);

                    if (CheckIfSpaceIsFreeToBuild(_ins) == false) {
                        DeleteLastElement();
                    }
                    
                }
            }
        }
    }

    public bool CheckIfSpaceIsFreeToBuild(GameObject _go) {
        if (collisionDetectorOn == false) return true;
        
        int _count = _go.transform.childCount;
        for (int i = 0; i < _count; i++) {
            if (_go.transform.GetChild(i).GetComponent<BoxCollider>() != null) {
                if (_go.transform.GetChild(i).tag == "SpaceNode") {
                    
                    Vector3 _halfSize = _go.transform.GetChild(i).GetComponent<BoxCollider>().size / 2f;
                    Vector3 _center = _go.transform.GetChild(i).transform.position;
                    Vector3 _orientation = _go.transform.GetChild(i).transform.rotation.eulerAngles;
                    RaycastHit[] hits = Physics.BoxCastAll(_center, _halfSize, Vector3.forward, Quaternion.Euler(_orientation), 0.01f);

                    foreach (RaycastHit hit in hits) {
                        if (hit.transform.parent != null) {
                            if (hit.transform.parent.gameObject != _go) {
                                if (hit.transform.tag == "SpaceNode") {
                                    Debug.Log(hit.transform.name);
                                    return false;

                                }
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    public void DeleteLastElement() {
        if (builtObjects.Count == 0) return;

        GameObject obj = builtObjects[builtObjects.Count - 1];


        if(obj.transform.GetComponent<PrefabInfo>() != null) // Ponowna aktywacja BoxCollidera, który był zapisany w prefabInfo jako rodzic
            if (obj.transform.GetComponent<PrefabInfo>().parentObj != null)
                obj.transform.GetComponent<PrefabInfo>().parentObj.GetComponent<BoxCollider>().enabled = true;

        if (obj.transform.GetComponent<PrefabInfo>() != null) // Ponowna aktywacja BoxCollidera dla każdego elementu, który jest dzieckiem elementu usuwanego
            if (obj.transform.GetComponent<PrefabInfo>().childObj != null) {
                PrefabInfo _prefabInfo = obj.transform.GetComponent<PrefabInfo>();
                for (int i = _prefabInfo.childObj.Count - 1; i >= 0; i--) {
                    if (_prefabInfo.childObj[i] != null) {
                        _prefabInfo.childObj[i].transform.GetChild(1).GetComponent<BoxCollider>().enabled = true;
                    }
                }
            }

        prefabCount[obj.transform.GetComponent<PrefabInfo>().buildID]--;
        Destroy(obj.transform.gameObject); // Kasowanie obiektu
        builtObjects.RemoveAt(builtObjects.Count - 1);
    }

    // Załadowanie sceny od nowa
    public void LoadSceneAgain() {
        SceneManager.LoadScene(0);
    }

    public void RemoveAllElement() {
        while (builtObjects.Count > 0) {
            DeleteLastElement();
        }
    }


    // Zmiana kolejnego elementu, który chcemy zbudować
    public void ChangeBuildId(int value) {
        prefabId = value;

        // Wyłączenie wszystkich obiektów które pokazują możliwą opcję budowy
        for (int i = 0; i < GameObject.Find("Temp").transform.childCount; i++) {
            GameObject.Find("Temp").transform.GetChild(i).gameObject.SetActive(false);
        }

        // Włączenie odpowiedniego obiektu pokazującego możliwą opcję budowy.
        GameObject.Find("Temp").transform.GetChild(value).gameObject.SetActive(true);
    }
    
    // Zmiana trybu budowania (budowanie w dowolnych miejscach / budowanie tylko na końcu jakiegoś obiektu)
    public void ChangeFreePlacingObjects() {
        if (mouseMesh == null) return;
        bool _val = GameObject.Find("Toggle").GetComponent<Toggle>().isOn;
        freePlacingObjects = _val;
        mouseMesh.gameObject.SetActive(_val);
    }

    public void ChangeCollisionDetector() {
        bool _val = GameObject.Find("Toggle1").GetComponent<Toggle>().isOn;
        collisionDetectorOn = _val;
    }

    // Pobieranie wartości dla wysokości stawianego elementu
    public void ChangeHeight(string _value) {
        height = float.Parse(GameObject.Find("ChangeHeight").GetComponent<InputField>().text);
    }

    // Pobieranie wartości rotacji w osi X
    public void ChangeXRot(string _value) {
        rotX = float.Parse(GameObject.Find("ChangeXRot").GetComponent<InputField>().text);
    }

    // Pobieranie wartości rotacji w osi Y
    public void ChangeYRot(string _value) {
        rotY = float.Parse(GameObject.Find("ChangeYRot").GetComponent<InputField>().text);
    }

    // Pobieranie wartości rotacji w osi Z
    public void ChangeZRot(string _value) {
        rotZ = float.Parse(GameObject.Find("ChangeZRot").GetComponent<InputField>().text);
    }

    // Generowanie kosztorysu
    public void SetKosztorys() {
        kosztorys.SetActive(!kosztorys.activeSelf);

        if (kosztorys.activeSelf == true) {
            // Zniszczenie dzieci "kosztorys_content" jeżeli takie posiada
            for (int i = kosztorys_content.transform.childCount - 1; i >= 0; i--) {
                Destroy(kosztorys_content.transform.GetChild(i).gameObject);
            }
            int totalCost = 0;
            prefabCount[8] += prefabCount[5] + prefabCount[7];
            prefabCount[10] += prefabCount[6] + prefabCount[9];
            // Tworzenie dzieci dla obiektu "kosztorys_content" i nadawanie im wartości.
            for (int i = 0; i < prefabs.Count; i++) {
                if (i == 5 || i == 7) continue;
                if (i == 6 || i == 9) continue;


                GameObject _ins = Instantiate(UI_obj, kosztorys_content.transform);
                _ins.transform.GetChild(1).GetComponent<Image>().sprite = sprites[i];
                _ins.transform.GetChild(2).GetComponent<Text>().text = prefabName[i];
                _ins.transform.GetChild(3).GetComponent<Text>().text = prefabCount[i] + "";
                _ins.transform.GetChild(4).GetComponent<Text>().text = cost[i] + "";
                _ins.transform.GetChild(5).GetComponent<Text>().text = (cost[i] * prefabCount[i]) + "";
                totalCost += cost[i] * prefabCount[i];

            }
            prefabCount[8] -= prefabCount[5] + prefabCount[7];
            prefabCount[10] -= prefabCount[6] + prefabCount[9];
            GameObject _ins1 = Instantiate(UI_obj2, kosztorys_content.transform);
            _ins1.transform.GetChild(1).GetComponent<Text>().text =totalCost + "";
        }

    }

    // Tworzenie pomieszczenia
    public void GenerateRoom() {
        roomGenerated = true;
        // Pobieranie danych o wielkości pomieszczenia
        roomX = float.Parse(GameObject.Find("Dlugosc").transform.GetChild(2).GetComponent<Text>().text);
        roomY = float.Parse(GameObject.Find("Wysokosc").transform.GetChild(2).GetComponent<Text>().text);
        roomZ = float.Parse(GameObject.Find("Szerokosc").transform.GetChild(2).GetComponent<Text>().text);

        // Zmiana skali ścian i podłogi
        GameObject.Find("WallX").transform.localScale = new Vector3(roomX, roomY, roomZ);
        GameObject.Find("WallZ").transform.localScale = new Vector3(roomX, roomY, roomZ);
        GameObject.Find("Floor").transform.localScale = new Vector3(roomX / 10f, 1, roomZ / 10f);
        GameObject.Find("Rozmiary Pomieszczenia").SetActive(false);

        // Zmiana skali tekstur ścian i podłogi
        matFloor.mainTextureScale = new Vector2(roomX / 2f, roomZ / 2f);
        matWallX.mainTextureScale = new Vector2(roomZ / 2f, roomY / 2f);
        matWallZ.mainTextureScale = new Vector2(roomX /2f, roomY/ 2f);
    }
}