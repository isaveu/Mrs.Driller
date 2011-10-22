using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum Direction {
    Left, Right, Down, Up
}

public class BlockController : MonoBehaviour {
    public float gravity = 5.0f;
    public float shakeTime = 0.5f;

    public float blinkTime = 0.75f;

    public float cameraFixed = 6.0f;

    public int numBlockRows = 100;
    public int numBlockCols = 15;

    public GameObject blockPrefab;
    
    public float blockSize {
        get { return this.blockPrefab.transform.localScale.x; }
    }

    Block.Type[,] hitTable;
    Block[,] fixedBlocks;
    List<Block> unbalanceBlocks;

    GameObject playerObj;
    Player player;

    public static readonly Dictionary<Direction, Vector2> Offset =
        new Dictionary<Direction, Vector2>() {
        { Direction.Left,  new Vector2(-1,  0) },
        { Direction.Right, new Vector2( 1,  0) },
        { Direction.Up,    new Vector2( 0, -1) },
        { Direction.Down,  new Vector2( 0,  1) }
    };

    public Block.Type Collision(Vector2 pos, Direction? direction = null) {
        if (direction != null) 
            pos += Offset[(Direction)direction];

        int row = Row(pos.y);
        int col = Col(pos.x);
        if (row == -1 || col == -1) {
            return Block.Type.Empty;
        } else {
            return this.hitTable[row, col];
        }
    }

    public Block BlockAtPos(Vector2 pos) {
        Block.Type type = Collision(pos);
        if (type == Block.Type.Empty) {
            return null;
        } else {
            int row = Row(pos.y);
            int col = Col(pos.x);
            Block block = this.fixedBlocks[row, col];
            block.type = type;
            block.pos = new Vector2(row, col);
            return block;
        }
    }

    public Block NextBlock(Vector2 pos, Direction direction) {
        pos += Offset[direction];
        return BlockAtPos(pos);
    }

    public void RemoveAtPos(Vector2 pos) {
        if (Collision(pos) == Block.Type.Empty) return;

        int row = Row(pos.y);
        int col = Col(pos.x);
        if (row == -1 || col == -1) return;

        Block block = this.fixedBlocks[row, col];
        Block.Group group = block.group;

        foreach (Block member in group) {
            row = Row(member.pos.y);
            col = Col(member.pos.x);
            if (row == -1 || col == -1) continue;

            UnFixed(row, col);
            member.type = Block.Type.Empty;
        }

        foreach (Block.Group upperGroup in group.LookUpUpperGroups()) {
            SetUnbalanceGroups(upperGroup);
        }
    }

    void Awake() {
        int numBlocks = this.numBlockRows * this.numBlockCols;

        this.hitTable = new Block.Type[this.numBlockRows, this.numBlockCols];
        this.fixedBlocks = new Block[this.numBlockRows, this.numBlockCols];
        this.unbalanceBlocks = new List<Block>(numBlocks / 2);

        int typesCount = System.Enum.GetValues(typeof(Block.Type)).Length;

        // random test data setting
        for (int row = 0; row < this.numBlockRows; row++) {
            for (int col = 0; col < this.numBlockCols; col++) {
                Block.Type type;
                if (row > 5) {
                    type = (Block.Type)Random.Range(0, typesCount);
                } else {
                    type = Block.Type.Empty;
                }

                this.hitTable[row, col] = type;

                if (type != Block.Type.Empty) {
                    GameObject blockObj = Instantiate(
                        this.blockPrefab, Vector2.zero, Quaternion.identity
                    ) as GameObject;
                    
                    Block block = blockObj.GetComponent<Block>();
                    block.pos = new Vector2(col, row);
                    block.type = type;
                    this.fixedBlocks[row, col] = block;
                }
            }
        }

        // Grouping all
        for (int row = 0; row < this.numBlockRows; row++) {
            for (int col = 0; col < this.numBlockCols; col++) {
                if (this.hitTable[row, col] == Block.Type.Empty) continue;

                Block block = this.fixedBlocks[row, col];
                if (block.group != null) continue;

                Block.Group group = new Block.Group(this);
                group.Grouping(block);
            }
        }
    }

    // Use this for initialization
    void Start() {
        this.playerObj = GameObject.Find("Player");
        this.player = playerObj.GetComponent<Player>();
    }

    void Update() {
        // check
        // for (int row = 0; row < this.numBlockRows; row++) {
        //     for (int col = 0; col < this.numBlockCols; col++) {
        //         Block.Type type = this.hitTable[row, col];
        //         Block block = this.fixedBlocks[row, col];
                
        //     }
        // }
    }
    
    // Update is called once per frame
    void LateUpdate() {
        playerObj.transform.position = ScreenPos(player.pos);

        float cameraDiff = player.pos.y - this.cameraFixed;
        if (cameraDiff > 0) {
            playerObj.transform.Translate(0, cameraDiff, 0);
        }

        foreach (GameObject blockObj in
                 GameObject.FindGameObjectsWithTag("Block")) {
            
            Block block = blockObj.GetComponent<Block>();

            blockObj.transform.position = ScreenPos(block.pos);
            
            if (cameraDiff > 0) {
                blockObj.transform.Translate(0, cameraDiff, 0);
            }
        }
    }

    Vector2 ScreenPos(Vector2 pos) {
        return new Vector2(pos.x, -pos.y);
    }

    int Row(float y) {
        int row = Mathf.FloorToInt(y);
        if (row < 0 || row > this.numBlockRows - 1) {
            return -1;
        } else {
            return row;
        }
    }

    int Col(float x) {
        int col = Mathf.FloorToInt(x);
        if (col < 0 || col > this.numBlockCols - 1) {
            return -1;
        } else {
            return col;
        }
    }

    void UnFixed(int row, int col) {
        this.hitTable[row, col] = Block.Type.Empty;

        Block block = this.fixedBlocks[row, col];
        block.pos = new Vector2(col, row);
        this.fixedBlocks[row, col] = null;
        block.DropStart(this.gravity);
    }

    void Fixed(int row, int col, Block block) {
        this.hitTable[row, col] = block.type;
        block.pos = new Vector2(col, row);
        block.DropEnd();
    }

    void SetUnbalanceGroups(Block.Group group) {
        HashSet<Block.Group> unbalanceGroups = group.LookUpUnbalanceGroups();
        foreach (Block.Group g in unbalanceGroups) {
            foreach (Block member in g) {
                this.unbalanceBlocks.Add(member);
                member.ShakeStart(this.shakeTime);
            }
        }

        this.unbalanceBlocks.Sort(delegate(Block a, Block b) {
                return (a.pos.y < b.pos.y ? 1 : -1);
            });
    }
}
