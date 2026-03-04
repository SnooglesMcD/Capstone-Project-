using UnityEngine;

public class GridManager : MonoBehavior
{
    [SerislizeField] private int _width, _height;

    [SerislizeField] private Tile _tilePrefab;

    void Generate Grid (){
        for (int x = 0; x < _width; x++){
            for (int y = 0; y < _height; y++){

            }
        }
    }
}
