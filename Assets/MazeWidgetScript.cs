using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeWidgetScript : MonoBehaviour
{
    [SerializeField]
    private TextMesh _text;
    [SerializeField]
    private GameObject _light;
    private KMBombInfo _info;

    private Indicator _label;
    private bool _isOn, _inputActive;
    private Vector2Int _lightPosition = new Vector2Int();
    private List<char> _displays;
    private List<bool> _walls;

    private const float MOVEDISTANCE = 0.025f;
    private const float MOVEDELAY = 0.25f;

    private void Awake()
    {
        _info = GetComponent<KMBombInfo>();

        Indicator[] labels = Enumerable.Range(0, 11).Select(i => (Indicator)i).Where(i => !_info.IsIndicatorPresent(i)).ToArray();
        if(labels.Length <= 0)
            _label = Indicator.NLL;
        else
            _label = labels.PickRandom();

        _isOn = UnityEngine.Random.Range(0, 2) == 0;

        _light.SetActive(true);
        _light.GetComponent<MeshRenderer>().material.color = _isOn ? new Color(.9f, .9f, .9f) : new Color(.1f, .1f, .1f);

        _displays = (_label.ToString() + "     ").ToList().Shuffle();

        Debug.LogFormat("[Maze Indicator] Generated with label: {0}, state: {1}.", _label, _isOn ? "on" : "off");
        _text.text = "";

        List<Vector2Int> visited = new List<Vector2Int> { _lightPosition };
        List<Vector2Int> allCells = Enumerable.Range(-1, 3).SelectMany(x => Enumerable.Range(-1, 3).Select(y => new Vector2Int(x, y))).ToList();
        allCells.Remove(new Vector2Int(-1, 1));
        List<Vector2Int> directions = new List<Vector2Int> { new Vector2Int(-1, 0), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(0, 1) };
        _walls = Enumerable.Range(0, 10).Select(x => false).ToList();
        while(visited.Count < 8)
        {
            Vector2Int newCell = allCells.Where(c => !visited.Contains(c) && directions.Any(dir => visited.Contains(c + dir))).PickRandom();
            Vector2Int newDir = directions.Where(d => visited.Contains(newCell + d)).PickRandom();
            visited.Add(newCell);
            _walls[PosDirToWallID(newCell, newDir)] = true;
        }

        GetComponent<KMWidget>().OnWidgetActivate += Activate;
        GetComponent<KMWidget>().OnQueryRequest += Query;
    }

    private int PosDirToWallID(Vector2Int pos, Vector2Int dir)
    {
        switch(pos.x)
        {
            case -1:
                switch(pos.y)
                {
                    case -1:
                        if(dir == new Vector2Int(0, 1))
                            return 5;
                        return 8;
                    case 0:
                        if(dir == new Vector2Int(0, -1))
                            return 5;
                        return 3;
                }
                break;
            case 0:
                switch(pos.y)
                {
                    case -1:
                        if(dir == new Vector2Int(0, 1))
                            return 6;
                        if(dir == new Vector2Int(-1, 0))
                            return 8;
                        return 9;
                    case 0:
                        if(dir == new Vector2Int(0, 1))
                            return 1;
                        if(dir == new Vector2Int(0, -1))
                            return 6;
                        if(dir == new Vector2Int(-1, 0))
                            return 3;
                        return 4;
                    case 1:
                        if(dir == new Vector2Int(0, -1))
                            return 1;
                        return 0;
                }
                break;
            case 1:
                switch(pos.y)
                {
                    case -1:
                        if(dir == new Vector2Int(0, 1))
                            return 7;
                        return 9;
                    case 0:
                        if(dir == new Vector2Int(0, 1))
                            return 2;
                        if(dir == new Vector2Int(-1, 0))
                            return 4;
                        return 7;
                    case 1:
                        if(dir == new Vector2Int(0, -1))
                            return 2;
                        return 0;
                }
                break;
        }
        return -1;
    }

    private string Query(string queryKey, string queryInfo)
    {
        Debug.LogFormat("<Maze Indicator> Queried with key: \"{1}\", info: {0}", queryInfo, queryKey);

        if(queryKey != KMBombInfo.QUERYKEY_GET_INDICATOR)
            return null;

        return "{\"label\":\"" + _label + "\",\"on\":\"" + _isOn + "\"}";
    }

    private void Activate()
    {
        _inputActive = true;
        DisplayText(new Vector2Int());
    }

    private void Update()
    {
        if(!_inputActive)
            return;
        if(Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.I))
            Move(new Vector2Int(0, 1));
        if(Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.K))
            Move(new Vector2Int(0, -1));
        if(Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.J))
            Move(new Vector2Int(-1, 0));
        if(Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.L))
            Move(new Vector2Int(1, 0));
    }

    private void Move(Vector2Int direction)
    {
        Vector2Int targetPosition = _lightPosition + direction;
        if(targetPosition.x > 1 || targetPosition.x < -1 || targetPosition.y > 1 || targetPosition.y < -1 || targetPosition.x == -1 && targetPosition.y == 1)
            return;

        if(PosDirToWallID(_lightPosition, direction) == -1)
            return;
        if(!_walls[PosDirToWallID(_lightPosition, direction)])
            return;

        _lightPosition = targetPosition;

        StopAllCoroutines();
        StartCoroutine(MoveToPosition());

        DisplayText(targetPosition);
    }

    private void DisplayText(Vector2Int targetPosition)
    {
        int ix = -1;
        switch(targetPosition.x)
        {
            case -1:
                switch(targetPosition.y)
                {
                    case -1:
                        ix = 0;
                        break;
                    case 0:
                        ix = 1;
                        break;
                }
                break;
            case 0:
                switch(targetPosition.y)
                {
                    case -1:
                        ix = 2;
                        break;
                    case 0:
                        ix = 3;
                        break;
                    case 1:
                        ix = 4;
                        break;
                }
                break;
            case 1:
                switch(targetPosition.y)
                {
                    case -1:
                        ix = 5;
                        break;
                    case 0:
                        ix = 6;
                        break;
                    case 1:
                        ix = 7;
                        break;
                }
                break;
        }
        if(ix != -1)
            _text.text = _displays[ix].ToString();
    }

    private IEnumerator MoveToPosition()
    {
        Vector3 startPos = _light.transform.localPosition, endPos = new Vector3(_lightPosition.x * MOVEDISTANCE, -0.005f, _lightPosition.y * MOVEDISTANCE);
        float time = Time.time;
        while(Time.time - time < MOVEDELAY)
        {
            _light.transform.localPosition = Vector3.Lerp(startPos, endPos, (Time.time - time) / MOVEDELAY);
            yield return null;
        }
        _light.transform.localPosition = endPos;
    }
}
