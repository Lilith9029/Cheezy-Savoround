using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum ActiveBooster
{
    None,
    Cutter,
    Sauce,
    Board,
    Trash
}

public class BoosterManager : MonoBehaviour
{
    public static BoosterManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private ActiveBooster _activeBooster = ActiveBooster.None;

    [Header("UI Buttons")]
    [SerializeField] private Button _btnCutter;
    [SerializeField] private Button _btnSauce;
    [SerializeField] private Button _btnBoard;
    [SerializeField] private Button _btnTrash;

    [Header("UI Labels")]
    [SerializeField] private TextMeshProUGUI _lblCutter;
    [SerializeField] private TextMeshProUGUI _lblSauce;
    [SerializeField] private TextMeshProUGUI _lblBoard;
    [SerializeField] private TextMeshProUGUI _lblTrash;

    [Header("UI Count Badges")]
    [SerializeField] private TextMeshProUGUI _badgeCutter;
    [SerializeField] private TextMeshProUGUI _badgeSauce;
    [SerializeField] private TextMeshProUGUI _badgeBoard;
    [SerializeField] private TextMeshProUGUI _badgeTrash;

    private GridManager _gridManager;
    private MergeManager _mergeManager;
    private Camera _mainCamera;
    private Cell _firstSelectedCell;
    private const float BoardLiftAmount = 0.2f;

    public ActiveBooster CurrentActiveBooster => _activeBooster;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        _gridManager = FindFirstObjectByType<GridManager>();
        _mergeManager = FindFirstObjectByType<MergeManager>();

        if (_btnCutter != null) _btnCutter.onClick.AddListener(() => ToggleBooster(ActiveBooster.Cutter));
        if (_btnSauce != null) _btnSauce.onClick.AddListener(() => ToggleBooster(ActiveBooster.Sauce));
        if (_btnBoard != null) _btnBoard.onClick.AddListener(() => ToggleBooster(ActiveBooster.Board));
        if (_btnTrash != null) _btnTrash.onClick.AddListener(() => ToggleBooster(ActiveBooster.Trash));

        UpdateBoosterUI();

        UserDataManager.OnDataSaved += UpdateBoosterUI;
        UserDataManager.OnDataLoaded += UpdateBoosterUI;
        GameManager.OnStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        UserDataManager.OnDataSaved -= UpdateBoosterUI;
        UserDataManager.OnDataLoaded -= UpdateBoosterUI;
        GameManager.OnStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state != GameState.Playing)
            CancelActiveBooster();
    }

    public void UpdateBoosterUI()
    {
        if (UserDataManager.Instance == null) return;

        int countCutter = UserDataManager.Instance.GetBoosterCount("cutter");
        int countSauce = UserDataManager.Instance.GetBoosterCount("sauce");
        int countBoard = UserDataManager.Instance.GetBoosterCount("board");
        int countTrash = UserDataManager.Instance.GetBoosterCount("trash");

        if (_lblCutter != null) _lblCutter.text = "Cutter";
        if (_lblSauce != null) _lblSauce.text = "Sauce";
        if (_lblBoard != null) _lblBoard.text = "Board";
        if (_lblTrash != null) _lblTrash.text = "Trash";

        if (_badgeCutter != null) _badgeCutter.text = $"X{countCutter}";
        if (_badgeSauce != null) _badgeSauce.text = $"X{countSauce}";
        if (_badgeBoard != null) _badgeBoard.text = $"X{countBoard}";
        if (_badgeTrash != null) _badgeTrash.text = $"X{countTrash}";

        ResetButtonColors();
    }

    private void ResetButtonColors()
    {
        if (_btnCutter != null) _btnCutter.GetComponent<Image>().color = Color.white;
        if (_btnSauce != null) _btnSauce.GetComponent<Image>().color = Color.white;
        if (_btnBoard != null) _btnBoard.GetComponent<Image>().color = Color.white;
        if (_btnTrash != null) _btnTrash.GetComponent<Image>().color = Color.white;
    }

    private void ToggleBooster(ActiveBooster booster)
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        if (_firstSelectedCell != null)
        {
            _firstSelectedCell.ResetTile();
            _firstSelectedCell = null;
        }

        if (_activeBooster == booster)
        {
            CancelActiveBooster();
            return;
        }

        string boosterId = GetBoosterId(booster);
        if (UserDataManager.Instance != null && UserDataManager.Instance.GetBoosterCount(boosterId) <= 0)
        {
            Debug.LogWarning($"[BoosterManager] Out of {boosterId}!");
            return;
        }

        _activeBooster = booster;
        ResetButtonColors();

        Color selectedColor = new Color(0.6f, 1f, 0.6f, 1f);
        switch (_activeBooster)
        {
            case ActiveBooster.Cutter: if (_btnCutter != null) _btnCutter.GetComponent<Image>().color = selectedColor; break;
            case ActiveBooster.Sauce: if (_btnSauce != null) _btnSauce.GetComponent<Image>().color = selectedColor; break;
            case ActiveBooster.Board: if (_btnBoard != null) _btnBoard.GetComponent<Image>().color = selectedColor; break;
            case ActiveBooster.Trash: if (_btnTrash != null) _btnTrash.GetComponent<Image>().color = selectedColor; break;
        }
    }

    public void CancelActiveBooster()
    {
        if (_firstSelectedCell != null && _firstSelectedCell.currentPlate != null)
        {
            _firstSelectedCell.currentPlate.transform.position -= new Vector3(0, BoardLiftAmount, 0);
        }

        _activeBooster = ActiveBooster.None;
        if (_firstSelectedCell != null)
        {
            _firstSelectedCell.ResetTile();
            _firstSelectedCell = null;
        }
        ResetButtonColors();
    }

    private string GetBoosterId(ActiveBooster booster)
    {
        switch (booster)
        {
            case ActiveBooster.Cutter: return "cutter";
            case ActiveBooster.Sauce: return "sauce";
            case ActiveBooster.Board: return "board";
            case ActiveBooster.Trash: return "trash";
            default: return "";
        }
    }

    private void Update()
    {
        if (_activeBooster == ActiveBooster.None) return;
        if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelActiveBooster();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_gridManager == null) _gridManager = FindFirstObjectByType<GridManager>();
            if (_mainCamera == null || _gridManager == null) return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            int layerMask = 1 << 6;
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, layerMask))
            {
                Cell clickedCell = hit.collider.GetComponent<Cell>();
                if (clickedCell != null) HandleCellClick(clickedCell);
            }
        }
    }

    private void HandleCellClick(Cell cell)
    {
        switch (_activeBooster)
        {
            case ActiveBooster.Cutter: ExecuteCutter(cell); break;
            case ActiveBooster.Sauce: ExecuteSauce(cell); break;
            case ActiveBooster.Board: ExecuteBoard(cell); break;
            case ActiveBooster.Trash: ExecuteTrash(cell); break;
        }
    }

    private void ConsumeBooster(string boosterId)
    {
        if (UserDataManager.Instance != null)
            UserDataManager.Instance.AddBooster(boosterId, -1);

        if (AchievementManager.Instance != null)
            AchievementManager.Instance.TrackBoosterUsed();

        UpdateBoosterUI();
    }

    private void ExecuteCutter(Cell cell)
    {
        if (!cell.IsOccupied) return;
        PizzaPlate plate = cell.currentPlate.GetComponent<PizzaPlate>();
        if (plate == null || plate.IsBeingCleared || plate.IsFull || plate.SliceCount == 0) return;
        Cell emptyNeighbor = null;
        foreach (Cell neighbor in _gridManager.GetNeighbors(cell.x, cell.z))
        {
            if (neighbor != null && !neighbor.IsOccupied) { emptyNeighbor = neighbor; break; }
        }
        if (emptyNeighbor == null) return;
        string dominantType = GetDominantSliceType(plate);
        if (string.IsNullOrEmpty(dominantType)) return;
        int missingSlices = 6 - plate.SliceCount;
        PlateSpawner spawner = FindFirstObjectByType<PlateSpawner>();
        if (spawner == null || spawner.platePrefab == null || spawner.pizzaSlicePrefabs == null) return;
        GameObject newPlateObj = Instantiate(spawner.platePrefab, emptyNeighbor.transform.position, Quaternion.identity);
        if (newPlateObj.GetComponent<PlateSkin>() == null) newPlateObj.AddComponent<PlateSkin>();
        PizzaPlate newPlate = newPlateObj.GetComponent<PizzaPlate>();
        newPlate.GenerateSlots();
        emptyNeighbor.currentPlate = newPlateObj;
        int typeIndex = 0;
        int.TryParse(dominantType, out typeIndex);
        typeIndex = Mathf.Clamp(typeIndex - 1, 0, spawner.pizzaSlicePrefabs.Length - 1);
        GameObject slicePrefab = spawner.pizzaSlicePrefabs[typeIndex];
        for (int i = 0; i < missingSlices; i++)
        {
            GameObject sliceObj = ObjectPooler.Instance != null
                ? ObjectPooler.Instance.SpawnFromPool("PizzaSlice_" + dominantType, slicePrefab, Vector3.zero, Quaternion.identity)
                : Instantiate(slicePrefab);
            PizzaSlice slice = sliceObj.GetComponent<PizzaSlice>() ?? sliceObj.AddComponent<PizzaSlice>();
            slice.sliceType = dominantType;
            newPlate.AddSlice(slice);
        }
        ConsumeBooster("cutter");
        newPlate.PlayBounceAnimation();
        if (_mergeManager == null) _mergeManager = FindFirstObjectByType<MergeManager>();
        if (_mergeManager != null) _mergeManager.CheckMerges(emptyNeighbor);
        CancelActiveBooster();
    }

    private void ExecuteSauce(Cell cell)
    {
        if (!cell.IsOccupied) return;

        PizzaPlate plate = cell.currentPlate.GetComponent<PizzaPlate>();
        if (plate == null || plate.IsBeingCleared || plate.IsFull || plate.SliceCount == 0) return;

        string dominantType = GetDominantSliceType(plate);
        if (string.IsNullOrEmpty(dominantType)) return;

        int missingSlices = 6 - plate.SliceCount;

        PlateSpawner spawner = FindFirstObjectByType<PlateSpawner>();
        if (spawner == null || spawner.pizzaSlicePrefabs == null) return;

        int typeIndex = 0;
        int.TryParse(dominantType, out typeIndex);
        typeIndex = Mathf.Clamp(typeIndex - 1, 0, spawner.pizzaSlicePrefabs.Length - 1);
        GameObject slicePrefab = spawner.pizzaSlicePrefabs[typeIndex];

        for (int i = 0; i < missingSlices; i++)
        {
            GameObject sliceObj = ObjectPooler.Instance != null
                ? ObjectPooler.Instance.SpawnFromPool("PizzaSlice_" + dominantType, slicePrefab, Vector3.zero, Quaternion.identity)
                : Instantiate(slicePrefab);

            PizzaSlice slice = sliceObj.GetComponent<PizzaSlice>() ?? sliceObj.AddComponent<PizzaSlice>();
            slice.sliceType = dominantType;
            plate.AddSlice(slice);
        }

        ConsumeBooster("sauce");
        plate.PlayBounceAnimation();

        if (plate.IsFull && plate.IsAllSameType())
        {
            plate.ClearPlate(true);
        }
        else
        {
            if (_mergeManager == null) _mergeManager = FindFirstObjectByType<MergeManager>();
            if (_mergeManager != null) _mergeManager.CheckMerges(cell);
        }

        CancelActiveBooster();
    }

    private void ExecuteBoard(Cell cell)
    {
        if (!cell.IsOccupied) return;

        if (_firstSelectedCell == null)
        {
            _firstSelectedCell = cell;
            _firstSelectedCell.LiftTile(BoardLiftAmount, 0.4f);

            if (_firstSelectedCell.currentPlate != null)
            {
                Transform plate = _firstSelectedCell.currentPlate.transform;
                plate.position += new Vector3(0, BoardLiftAmount, 0);
            }
        }
        else if (_firstSelectedCell == cell)
        {
            if (_firstSelectedCell.currentPlate != null)
            {
                Transform plate = _firstSelectedCell.currentPlate.transform;
                plate.position -= new Vector3(0, BoardLiftAmount, 0);
            }

            _firstSelectedCell.ResetTile();
            _firstSelectedCell = null;
        }
        else
        {
            GameObject plateA = _firstSelectedCell.currentPlate;
            GameObject plateB = cell.currentPlate;

            if (plateA == null || plateB == null)
            {
                if (plateA != null)
                    plateA.transform.position -= new Vector3(0, BoardLiftAmount, 0);

                _firstSelectedCell.ResetTile();
                _firstSelectedCell = null;
                CancelActiveBooster();
                return;
            }

            plateA.transform.position -= new Vector3(0, BoardLiftAmount, 0);

            Cell cellA = _firstSelectedCell;
            Cell cellB = cell;

            cellA.currentPlate = plateB;
            cellB.currentPlate = plateA;

            plateB.transform.position = new Vector3(
                cellA.transform.position.x,
                plateB.transform.position.y,
                cellA.transform.position.z
            );
            plateA.transform.position = new Vector3(
                cellB.transform.position.x,
                plateA.transform.position.y,
                cellB.transform.position.z
            );

            plateA.GetComponent<PizzaPlate>()?.PlayBounceAnimation();
            plateB.GetComponent<PizzaPlate>()?.PlayBounceAnimation();

            ConsumeBooster("board");
            StartCoroutine(DelayedMergeCheck(cellA, cellB));

            _firstSelectedCell.ResetTile();
            _firstSelectedCell = null;
            CancelActiveBooster();
        }
    }


    private System.Collections.IEnumerator DelayedMergeCheck(Cell cellA, Cell cellB)
    {
        yield return new WaitForSeconds(0.3f);

        if (_mergeManager == null) _mergeManager = FindFirstObjectByType<MergeManager>();
        if (_mergeManager != null)
        {
            _mergeManager.CheckMerges(cellA);
            _mergeManager.CheckMerges(cellB);
        }
    }

    private void ExecuteTrash(Cell cell)
    {
        if (!cell.IsOccupied) return;
        GameObject plateObj = cell.currentPlate;
        PizzaPlate plate = plateObj.GetComponent<PizzaPlate>();
        if (plate == null || plate.IsBeingCleared) return;
        cell.currentPlate = null;
        if (ObjectPooler.Instance != null)
            ObjectPooler.Instance.SpawnFromPool("PizzaExplosion", plateObj.transform.position, Quaternion.identity);
        Destroy(plateObj);
        ConsumeBooster("trash");
        if (HoldSlotsManager.Instance != null) HoldSlotsManager.Instance.CheckAndRefillSlots();
        CancelActiveBooster();
    }

    private string GetDominantSliceType(PizzaPlate plate)
    {
        var slices = plate.GetAllSlices();
        if (slices == null || slices.Count == 0) return "";
        Dictionary<string, int> counts = new Dictionary<string, int>();
        foreach (var s in slices)
        {
            if (counts.ContainsKey(s.sliceType)) counts[s.sliceType]++;
            else counts[s.sliceType] = 1;
        }
        string dominant = "";
        int maxCount = -1;
        foreach (var pair in counts)
        {
            if (pair.Value > maxCount) { maxCount = pair.Value; dominant = pair.Key; }
        }
        return dominant;
    }
}