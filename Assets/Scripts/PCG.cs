using System;
using System.Collections.Generic;
using System.Drawing;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PCG : MonoBehaviour
{
	class Room
	{		
		public Room(int size, Vector2Int position)
		{
			m_startPosition = position;
			m_endPosition = new Vector2Int(0, 0);
			m_openingPosition = new Vector2Int(0, 0);
			m_size = size;
			m_wallCount = 0;
			m_IsDoorExist = false;
		}

		public Vector2Int m_startPosition;
		public Vector2Int m_endPosition;
		public Vector2Int m_openingPosition;
		public int m_size;
		public int m_wallCount;
		public bool m_IsDoorExist;
	}

    class Edge : IComparable<Edge>
    {
        public Edge(int from, int to, int distance)
        {
            this.From = from;
            this.To = to;
            this.Distance = distance;
        }

        public int From { get; private set; }
        public int To { get; private set; }
        private int Distance { get; set; }
        public int CompareTo(Edge edge)
        {
	        return this.Distance.CompareTo(edge.Distance);
        }
    }

    public int ManHatten(Vector2Int p1, Vector2Int p2)
    {
        return Math.Abs(p1.x - p2.x) + Math.Abs(p1.y - p2.y);
    }

    class UnionFind
    {
        private int[] parents;

        public UnionFind(int roomCnt)
        {
            parents = new int[roomCnt];

            for (int i = 0; i < roomCnt; i++)
            {
                parents[i] = i;
            }
        }

        private int Find(int a)
        {
            if (parents[a] == a) return a;

            return parents[a] = Find(parents[a]);
        }

        public bool Union(int a, int b)
        {
            int parentA = Find(a);
			int parentB = Find(b);

            if (parentA == parentB) return false;

			parents[parentA] = parentB;
            return true;
        }
    }

	public float GridSize = 3.0f; //Size of floor and wall tiles in units
	private int MaxMapSize = 41; //Maximum height and width of tile map
	private Dictionary<string, GameObject> Prefabs; //Dictionary of all PCG prefabs
	private GameObject[] TileMap; //Tilemap array to make sure we don't put walls over floors
	private int TileMapMidPoint; //The 0,0 point of the tile map array
	private System.Random RNG;
	
	private List<Room> roomList = new List<Room>();
	private List<Vector2Int> availableFloor = new List<Vector2Int>();
    private List<Edge> edgeList = new List<Edge>();
    private List<int> itemIndex = new List<int>();
	private List<int> enemyIndex = new List<int>();

	private List<Vector2Int> FloorList = new List<Vector2Int>();
	private List<Vector2Int> OneTileList = new List<Vector2Int>();
	private List<KeyValuePair<string, int>> itemList;
	private List<string> enemyList;
	private GameObject Hero;
	
	private int amountOfGoldDoors = 0;
	private bool portalCheck = false;

    [SerializeField] private int roomCnt = 35;


	private int bossEnemyCount = 0;

	// Start is called before the first frame update
	void Start()
	{
		//Load all the prefabs we need for map generation (note that these must be in a "Resources" folder)
		Prefabs = new Dictionary<string, GameObject>();
		Prefabs.Add("floor", Resources.Load<GameObject>("Prefabs/Floor"));
		Prefabs["floor"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
		Prefabs.Add("special", Resources.Load<GameObject>("Prefabs/FloorSpecial"));
		Prefabs["special"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the floor properly
		Prefabs.Add("wall", Resources.Load<GameObject>("Prefabs/Wall"));
		Prefabs["wall"].transform.localScale = new Vector3(GridSize, GridSize, 1.0f); //Scale the wall properly
		Prefabs.Add("portal", Resources.Load<GameObject>("Prefabs/Portal"));
		Prefabs.Add("weak", Resources.Load<GameObject>("Prefabs/WeakEnemy")); // add weak
		Prefabs.Add("enemy", Resources.Load<GameObject>("Prefabs/BaseEnemy"));
		Prefabs.Add("fast", Resources.Load<GameObject>("Prefabs/FastEnemy"));
		Prefabs.Add("spread", Resources.Load<GameObject>("Prefabs/SpreadEnemy"));
		Prefabs.Add("tank", Resources.Load<GameObject>("Prefabs/TankEnemy"));
		Prefabs.Add("tankboss", Resources.Load<GameObject>("Prefabs/TankBossEnemy"));
		Prefabs.Add("fastboss", Resources.Load<GameObject>("Prefabs/FastBossEnemy"));
		Prefabs.Add("ultra", Resources.Load<GameObject>("Prefabs/UltraEnemy"));
		Prefabs.Add("boss", Resources.Load<GameObject>("Prefabs/BossEnemy"));
		Prefabs.Add("dashabilityenemy", Resources.Load<GameObject>("Prefabs/DashAbilityEnemy"));
		Prefabs.Add("heart", Resources.Load<GameObject>("Prefabs/HeartPickup"));
		Prefabs.Add("healthboost", Resources.Load<GameObject>("Prefabs/HealthBoost"));
		Prefabs.Add("shotboost", Resources.Load<GameObject>("Prefabs/ShotBoost"));
		Prefabs.Add("shotspeedboost", Resources.Load<GameObject>("Prefabs/ShotSpeedBoost"));
		Prefabs.Add("speedboost", Resources.Load<GameObject>("Prefabs/SpeedBoost"));
		Prefabs.Add("shield", Resources.Load<GameObject>("Prefabs/Shield")); //add shield
		Prefabs.Add("silverkey", Resources.Load<GameObject>("Prefabs/SilverKey"));
		
		Prefabs.Add("goldkey", Resources.Load<GameObject>("Prefabs/GoldKey"));
		Prefabs["goldkey"].transform.localScale = new Vector3(3.0f, 3.0f, 1.0f);
		
		Prefabs.Add("silverdoor", Resources.Load<GameObject>("Prefabs/SilverDoor"));
		Prefabs["silverdoor"].transform.localScale = new Vector3(GridSize / 2.0f, 0.5f, 1.0f); //Scale the door properly
		
		Prefabs.Add("golddoor", Resources.Load<GameObject>("Prefabs/GoldDoor"));
		Prefabs["golddoor"].transform.localScale = new Vector3(GridSize / 2.0f, 3.0f, 1.0f); //Scale the door properly
		
		Prefabs.Add("exitdoor", Resources.Load<GameObject>("Prefabs/ExitDoor"));
		Prefabs["exitdoor"].transform.localScale = new Vector3(GridSize / 3.0f, 0.5f, 1.0f); //Scale the door properly

		//Delete everything visible except the hero when reloading       
		var objsToDelete = FindObjectsOfType<SpriteRenderer>();
		int totalObjs = objsToDelete.Length;
		for (int i = 0; i < totalObjs; i++)
		{
			if (objsToDelete[i].gameObject.ToString().StartsWith("Hero") == false)
				UnityEngine.Object.DestroyImmediate(objsToDelete[i].gameObject);
		}

		//Create the tile map
		RNG = new System.Random();

		int minimumValue = -(MaxMapSize / 2);
		int maximumValue = MaxMapSize / 2;

		for (int y = minimumValue; y < maximumValue; y++)
		{
			for (int x = minimumValue; x < maximumValue; x++)
			{
				availableFloor.Add(new Vector2Int(x, y));
			}
		}
		
		itemList = new List<KeyValuePair<string, int>>()
		{
			new KeyValuePair<string, int>("heart", 5),
			new KeyValuePair<string, int>("speedboost", 4),
			new KeyValuePair<string, int>("shield", 3),
			new KeyValuePair<string, int>("shotboost", 3),
			new KeyValuePair<string, int>("healthboost", 2),
			new KeyValuePair<string, int>("shotspeedboost", 3),
		};

		enemyList = new List<string>()
		{
			"tank",
			"ultra",
		};

		int itemCumulativeNumber = 0;
		for(int i = 0; i < itemList.Count; i++)
        {
			itemCumulativeNumber += itemList[i].Value;
			itemIndex.Add(itemCumulativeNumber);
		}

		int totalItemOptions = 0;
		for (int i = 0; i < itemList.Count; i++)
		{
			totalItemOptions += itemList[i].Value;
		}

		TileMap = new GameObject[MaxMapSize * MaxMapSize];
		TileMapMidPoint = (MaxMapSize * MaxMapSize) / 2;
		Hero = GameObject.Find("Hero");
		Hero.transform.position = new Vector2(minimumValue * 3, minimumValue * 3);

		//Create the starting tile
		roomList.Add(new Room(4, new Vector2Int(minimumValue, minimumValue)));
		MakeRoom(roomList[0], maximumValue, minimumValue);
		
		List<Vector2Int>[] quadrantFloor = new List<Vector2Int>[4]
		{
			new List<Vector2Int>(),
			new List<Vector2Int>(),
			new List<Vector2Int>(),
			new List<Vector2Int>()
		};

		foreach (var pos in availableFloor)
		{
			if(pos.y < 0 && pos.x > 0) //1사분면
				quadrantFloor[0].Add(pos);
			else if(pos.y < 0 && pos.x < 0) //2사분면
				quadrantFloor[1].Add(pos);
			else if(pos.y > 0 && pos.x < 0) //3사분면
				quadrantFloor[2].Add(pos);
			else quadrantFloor[3].Add(pos);
		}
		
		//add more pcg logic here..
		for(int i = 1; i < roomCnt; i++)
		{
			int qIndex = i % 4;
			var quadrant = quadrantFloor[qIndex];
			var spawnPos = quadrant[RNG.Next(quadrant.Count)];
			
			if (i < 5)
			{
				int roomSize = RNG.Next(5, 8);
				roomList.Add(new Room(roomSize, spawnPos));
				MakeCrossShapeRoom(roomList[i], maximumValue, minimumValue);
			}
			else if (i < 15)
			{
				int roomSize = RNG.Next(2, 4);
				roomList.Add(new Room(roomSize, spawnPos));
				MakeRoom(roomList[i], maximumValue, minimumValue); //square
			}
			else if (i < 25)
			{
				roomList.Add(new Room(3, spawnPos));
				MakeTriangleRoom(roomList[i], maximumValue, minimumValue);
			}
			else
            {
				roomList.Add(new Room(4, spawnPos));
				MakeDiamondRoom(roomList[i], maximumValue, minimumValue);
			}
		}

		//boss Room
		roomList.Add(new Room(7, new Vector2Int(14,14)));
		MakeBossRoom(roomList[roomCnt], maximumValue, minimumValue);

        for (int i = 0; i < roomList.Count; i++)
        {
            for (int j = i+1; j < roomList.Count; j++)
            {
				edgeList.Add(new Edge(i, j, ManHatten(roomList[i].m_startPosition, roomList[j].m_startPosition)));
            }
        }
		
        edgeList.Sort();

        UnionFind uf = new UnionFind(roomList.Count);
        
        foreach(Edge edge in edgeList)
        {
	        if (uf.Union(edge.From, edge.To))
	        {
		        ConnectRoom(roomList[edge.From], roomList[edge.To]);
	        }
        }
        
		FillEmptySpaceWithWall(minimumValue, maximumValue);
		FillEdgeWithWall(minimumValue, maximumValue, 1);

		for (int i = 0; i < roomList.Count; i++)
		{
			WallCount(roomList[i]);
		}

		SpawnDoor();

		int portalRoomIndex = SpawnPortalAndRoomItem(totalItemOptions);
		
		if(!portalCheck)
			Invoke("ResetLevel", 0f);

		GameObject PortalEnemy = new GameObject("PortalEnemy");
		for (int i = 1; i < roomList[portalRoomIndex].m_size; i++)
		{
			int enemyDice = RNG.Next(enemyList.Count);
			PortalEnemy = Spawn(enemyList[enemyDice], roomList[portalRoomIndex].m_startPosition.x + i, roomList[portalRoomIndex].m_startPosition.y);
			break;
		}
		PortalEnemy.name = "PortalEnemy";

		for(int i = 1; i < FloorList.Count; i++)
        {
			if (FloorList[i].x >= 2 && FloorList[i].x <= 7 && FloorList[i].y >= 2 && FloorList[i].y <= 7)
            {
				Spawn("dashabilityenemy", FloorList[i].x, FloorList[i].y);
				break;
            }
		}

		int amountOfItem = 16 - amountOfGoldDoors;
		for(int i = 0; i < amountOfItem; i++)
		{
			int randomDice = RNG.Next(FloorList.Count);

			if (FloorList[randomDice].x > -10)
		    {
				SpawnItem(new Vector2Int(FloorList[randomDice].x, FloorList[randomDice].y), totalItemOptions);
		    }
		    else
		    {
				i--;
		    }
		}

		SpawnMosterByRange();

		SpawnBossMonster();

	}
	private void ResetLevel()
	{
		var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
		SceneManager.LoadScene(currentSceneIndex);
	}

	void SpawnBossMonster()
    {
		int dice = RNG.Next(2);

		int PositionDice1 = RNG.Next(14, 21);
		int PositionDice2 = RNG.Next(14, 21);

		Spawn("boss", PositionDice1, PositionDice1);

		if (dice == 0)
		{
			Spawn("fastboss", PositionDice2, PositionDice2);
		}
		else if (dice == 1)
		{
			Spawn("tankboss", PositionDice2, PositionDice2);
		}

	}

	int SpawnPortalAndRoomItem(int totalItemOptions)
    {
		int roomIndex = 0;
		for (int i = 0; i < roomList.Count; i++)
		{
			if (roomList[i].m_IsDoorExist == true && portalCheck == true)
			{
				SpawnItem(roomList[i].m_startPosition, totalItemOptions);
			}

			if (roomList[i].m_IsDoorExist == true && portalCheck == false)
			{
				Spawn("portal", roomList[i].m_startPosition.x, roomList[i].m_startPosition.y);
				SpawnRotateRight("exitdoor", roomList[i].m_startPosition.x + 0.5f, roomList[i].m_startPosition.y);
				Spawn("exitdoor", roomList[i].m_startPosition.x, roomList[i].m_startPosition.y + 0.5f);
				roomIndex = i;
				portalCheck = true;
			}
		}
		return roomIndex;
	}

	void SpawnDoor()
	{
		for (int i = 0; i < roomList.Count; i++)
		{
			if (roomList[i].m_size == 2 && roomList[i].m_wallCount == 11)
			{
				if (roomList[i].m_openingPosition != roomList[i].m_startPosition)
				{
					Spawn("golddoor", roomList[i].m_openingPosition.x, roomList[i].m_openingPosition.y);

					roomList[i].m_IsDoorExist = true;
				}
			}

			if (roomList[i].m_size == 3 && roomList[i].m_wallCount == 15)
			{
				if (roomList[i].m_openingPosition != roomList[i].m_startPosition)
				{
					Spawn("golddoor", roomList[i].m_openingPosition.x, roomList[i].m_openingPosition.y);
					roomList[i].m_IsDoorExist = true;
				}
			}
		}

		for (int i = 0; i < roomList.Count; i++)
		{
			if (roomList[i].m_IsDoorExist == true)
			{
				amountOfGoldDoors++;
			}
		}


	}

	void WallCount(Room room)
    {
		Vector2Int Position = room.m_endPosition;
		int outerSize = room.m_size + 2;

		for (int y = outerSize; y > 0; y--) 
        {
			for (int x = outerSize; x > 0; x--) 
            {
				if (GetTile(Position.x, Position.y) == null || GetTile(Position.x, Position.y) == Prefabs["wall"])
				{
					room.m_wallCount++;
				}

				if (y < room.m_size + 2 && y > 1)
				{
					Position.x = Position.x - room.m_size - 1;

					if (GetTile(Position.x, Position.y) == null || GetTile(Position.x, Position.y) == Prefabs["wall"])
					{
						room.m_wallCount++;
					}

					x = 1;
					continue;
				}				
				Position.x--;
            }
			Position.x = room.m_endPosition.x;
			Position.y--;
        }
    }
	
	void SpawnMosterByRange()
	{
		int dice = 0;
		int[] enemyCount = new int[5];

		for(int i = 0; i < FloorList.Count; i++)
        {
			//d1
			if (FloorList[i].x > -14 && FloorList[i].x < -7 && FloorList[i].y >= -20 && FloorList[i].y < -16)
			{
				if (enemyCount[0] > 2)
					continue;

				Spawn("weak", FloorList[i].x, FloorList[i].y);
				enemyCount[0]++;

			}

			//d1
			if (FloorList[i].x >= -5 && FloorList[i].x < 0 && FloorList[i].y >= -14 && FloorList[i].y < -7)
			{
				if (enemyCount[1] > 2)
					continue;
			
				Spawn("enemy", FloorList[i].x, FloorList[i].y);
				enemyCount[1]++;
			
			}

			//develop2
			if (FloorList[i].x >= 2 && FloorList[i].x <= 7 && FloorList[i].y >= 2 && FloorList[i].y <= 7)
			{
				if (enemyCount[2] > 5)
					continue;
			
				dice = RNG.Next(2);
				if (dice == 0)
			    {
					Spawn("fast", FloorList[i].x, FloorList[i].y);
					enemyCount[2]++;
			    }
				else if (dice == 1)
			    {
					Spawn("spread", FloorList[i].x, FloorList[i].y);
					enemyCount[2]++;
			    }
			}

			//develop3
			if (FloorList[i].x >= 9 && FloorList[i].x <= 13 && FloorList[i].y >= 9 && FloorList[i].y <= 13)
			{
				if (enemyCount[3] > 3)
					continue;
			
				dice = RNG.Next(2);
				if (dice == 0)
			    {
					Spawn("tank", FloorList[i].x, FloorList[i].y);
					enemyCount[3]++;
			    }
				else if (dice == 1)
			    {
					Spawn("ultra", FloorList[i].x, FloorList[i].y);
					enemyCount[3]++;
			    }
			}

		}
	}

	void SpawnItem(Vector2Int ItemPosition, int totalItemOptions)
    {
		int diceNumber = RNG.Next(0, totalItemOptions + 1);

		if (diceNumber < itemIndex[0])
		{
			Spawn("heart", ItemPosition.x, ItemPosition.y);
		}
		else if (diceNumber < itemIndex[1])
		{
			Spawn("shield", ItemPosition.x, ItemPosition.y);
		}
		else if (diceNumber < itemIndex[2])
		{
			Spawn("speedboost", ItemPosition.x, ItemPosition.y);
		}
		else if (diceNumber < itemIndex[3])
		{
			Spawn("shotboost", ItemPosition.x, ItemPosition.y);
		}
		else if (diceNumber < itemIndex[4])
		{
			Spawn("healthboost", ItemPosition.x, ItemPosition.y);
		}
		else if (diceNumber < itemIndex[5])
		{
			Spawn("shotspeedboost", ItemPosition.x, ItemPosition.y);
		}
	}

	void MakeDiamondRoom(Room room, int maximumValue ,int minimumValue)
	{
		Vector2Int position = room.m_startPosition;

		if (room.m_startPosition.x > maximumValue || room.m_startPosition.x < minimumValue)
			return;

		if (room.m_startPosition.y > maximumValue || room.m_startPosition.y < minimumValue)
			return;

		int xRoomSize = 1;
		int xIndex = 1;
		for (int y = 0; y < room.m_size; y++)
		{
			for (int x = 0; x < xRoomSize; x++)
			{
				SpawnTile(position.x, position.y);
				position.x++;
			}
			position.x = room.m_startPosition.x - xIndex;
			xIndex++;
			xRoomSize += 2;
			position.y--;
		} //upper triangle

		position.y -= 3;
		xRoomSize = 1;
		xIndex = 1;
		position.x = room.m_startPosition.x;

		for (int y = 0; y < room.m_size; y++)
		{
			for (int x = 0; x < xRoomSize; x++)
			{
				SpawnTile(position.x, position.y);
				position.x++;
			}
			position.x = room.m_startPosition.x - xIndex;
			xIndex++;
			xRoomSize += 2;
			position.y++;
		}

		room.m_openingPosition = room.m_startPosition;

	}

	void MakeTriangleRoom(Room room, int maximumValue, int minimumValue)
    {
		Vector2Int position = room.m_startPosition;

		if (room.m_startPosition.x > maximumValue || room.m_startPosition.x < minimumValue)
			return;

		if (room.m_startPosition.y > maximumValue || room.m_startPosition.y < minimumValue)
			return;

		int xRoomSize = 1;
		int xIndex = 1;
		for(int y = 0; y < room.m_size; y++)
        {
			for(int x = 0; x < xRoomSize; x++)
            {
				SpawnTile(position.x, position.y);
				position.x++;
            }
			position.x = room.m_startPosition.x - xIndex;
			xIndex++;
			xRoomSize += 2;
			position.y--;
        }

		room.m_openingPosition = room.m_startPosition;
	}

	void MakeCrossShapeRoom(Room room, int maximumValue, int minimumValue)
    {
		Vector2Int position = room.m_startPosition;
		int offset = room.m_size / 2;
		if (room.m_startPosition.x > maximumValue || room.m_startPosition.x < minimumValue)
			return;

		if (room.m_startPosition.y > maximumValue || room.m_startPosition.y < minimumValue)
			return;

		for (int y = 0; y < room.m_size; y++)
        {
			SpawnTile(position.x, position.y);
			position.y++;
        }
		
		position = room.m_startPosition;
		position.x -= offset;
		position.y += offset;

		for (int x = 0; x < room.m_size; x++)
        {
			SpawnTile(position.x, position.y);
			position.x++;
        }
		SpawnTile(position.x, position.y);

		if (position.x > maximumValue || position.y > maximumValue)
		{
			room.m_openingPosition = room.m_startPosition;
		}
		else
		{
			room.m_openingPosition = position;
		}
    }

	void MakeBossRoom(Room room, int maximumValue, int minimumValue)
    {
		Vector2Int position = room.m_startPosition;
		Vector2Int wallPosition = new Vector2Int(14, 20);

		for (int y = 0; y < room.m_size; y++)
		{
			for (int x = 0; x < room.m_size; x++)
			{
				if (room.m_startPosition.x > maximumValue || room.m_startPosition.x < minimumValue)
					break;

				SpawnSpecialTile(position.x, position.y);

				position.x++;
			}
			if (y == room.m_size - 1)
			{
				room.m_endPosition = new Vector2Int(position.x, position.y + 1);
				break;
			}
			position.y++;
			position.x = room.m_startPosition.x;
		}

		if (position.x > maximumValue || position.y > maximumValue)
		{
			room.m_openingPosition = room.m_startPosition;
		}
		else
		{
			room.m_openingPosition = position;
		}

		//make wall to the boss room
		for (int y = room.m_size; y > 0; y--)
		{
			SpawnRotateLeft("silverdoor", wallPosition.x - 0.5f, wallPosition.y);
			wallPosition.y--;
		}

		for (int x = 0; x < roomList[35].m_size; x++)
		{
			Spawn("silverdoor", wallPosition.x, wallPosition.y + 0.5f);
			wallPosition.x++;
		}
	}

	void MakeRoom(Room room, int maximumValue, int minimumValue)
	{
		Vector2Int position = room.m_startPosition;
		for (int y = 0; y < room.m_size; y++)
		{
			for (int x = 0; x < room.m_size; x++)
			{
				if (room.m_startPosition.x > maximumValue || room.m_startPosition.x < minimumValue)
					break;

				SpawnTile(position.x, position.y);

				position.x++;
			}
			if (y == room.m_size - 1)
			{
				room.m_endPosition = new Vector2Int(position.x, position.y + 1);
				break;
			}
			position.y++;
			position.x = room.m_startPosition.x;
		}
		SpawnTile(position.x, position.y);

		if (position.x > maximumValue || position.y > maximumValue)
		{
			room.m_openingPosition = room.m_startPosition;
		}
		else
		{
			room.m_openingPosition = position;
		}
	}

	void ConnectRoom(Room room1, Room room2)
    {
		Vector2Int distance = new Vector2Int();
		Vector2Int compareX = new Vector2Int();
		Vector2Int compareY = new Vector2Int();

		Vector2Int roomOnePosition = room1.m_openingPosition;
		Vector2Int roomTwoPosition = room2.m_openingPosition;

		if (roomOnePosition.x == roomTwoPosition.x)
		{
			if (roomOnePosition.y > roomTwoPosition.y)
			{
				distance.y = roomOnePosition.y - roomTwoPosition.y;
				compareY = roomTwoPosition;
			}
			else if (roomOnePosition.y < roomTwoPosition.y)
			{
				distance.y = roomTwoPosition.y - roomOnePosition.y;
				compareY = roomOnePosition;
			}

			for (int y = 0; y < Math.Abs(distance.y); y++)
			{				
				compareY.y++;
				SpawnTile(compareY.x, compareY.y);
				OneTileList.Add(compareY);
			}
			return;
		}

		if (roomOnePosition.y == roomTwoPosition.y)
		{
			if (roomOnePosition.x > roomTwoPosition.x)
			{
				distance.x = roomOnePosition.x - roomTwoPosition.x;
				compareX = roomTwoPosition;
			}
			else if (roomOnePosition.x < roomTwoPosition.x)
			{
				distance.x = roomTwoPosition.x - roomOnePosition.x;
				compareX = roomOnePosition;
			}

			for (int x = 0; x < Math.Abs(distance.x); x++)
			{
				compareX.x++;
				SpawnTile(compareX.x, compareX.y);
				OneTileList.Add(compareX);
			}
			return;
		}

		if (roomOnePosition.x > roomTwoPosition.x)
		{
			distance.x = roomOnePosition.x - roomTwoPosition.x;
			compareX = roomTwoPosition;
		}
		else if (roomOnePosition.x < roomTwoPosition.x)
		{
			distance.x = roomTwoPosition.x - roomOnePosition.x;
			compareX = roomOnePosition;
		}
		for (int x = 0; x < Math.Abs(distance.x); x++)
		{
			compareX.x++;
			SpawnTile(compareX.x, compareX.y);
			OneTileList.Add(new Vector2Int(compareX.x, compareX.y));
		}

		if (roomOnePosition.y > roomTwoPosition.y)
		{
			distance.y = roomOnePosition.y - roomTwoPosition.y;
			compareY = roomTwoPosition;
		}
		else if (roomOnePosition.y < roomTwoPosition.y)
		{
			distance.y = roomTwoPosition.y - roomOnePosition.y;
			compareY = roomOnePosition;
		}
		for (int y = 0; y < Math.Abs(distance.y); y++)
		{
			compareY.y++;
			SpawnTile(compareX.x, compareY.y);
			OneTileList.Add(new Vector2Int(compareX.x, compareX.y));
		}
	}

	void FillEdgeWithWall(int minimumValue_, int maximumValue_, int layer)
    {
		for(int y = minimumValue_ - layer; y <= maximumValue_ + layer; y++)
        {
			for(int x = minimumValue_ - layer; x <= maximumValue_ + layer; x++)
            {
				if (y < minimumValue_ || y > maximumValue_)
				{
					Spawn("wall", x, y);
				}
				else {
					if (x < minimumValue_ || x > maximumValue_)
					{
						Spawn("wall", x, y);
					}
				}
			}
		}
    }

	void FillEmptySpaceWithWall(int minimumValue_, int maximumValue_)
    {
		int index = 0;

		if (index < 0)
			index = 0;

		for (int y = minimumValue_; y < maximumValue_ + 1; y++)
		{
			for (int x = minimumValue_; x < maximumValue_ + 1; x++)
			{
				if (TileMap[index] == null)
				{
					Spawn("wall", x, y);
				}
				index++;
			}
		}
	}

	//Get a tile object (only walls and floors, currently)
	GameObject GetTile(int x, int y)
	{
		if (Math.Abs(x) > MaxMapSize / 2 || Math.Abs(y) > MaxMapSize/2)
			return Prefabs["wall"];
		return TileMap[(y * MaxMapSize) + x + TileMapMidPoint];
	}

	//Spawn a tile object if one isn't already there
	void SpawnTile(int x, int y)
	{
		if (GetTile(x, y) != null)
			return;

		TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn("floor", x, y);
		
		availableFloor.Remove(new Vector2Int(x, y));
		
		FloorList.Add(new Vector2Int(x, y));
	}

	void SpawnSpecialTile(int x, int y)
	{
		TileMap[(y * MaxMapSize) + x + TileMapMidPoint] = Spawn("special", x, y);

		availableFloor.Remove(new Vector2Int(x, y));

		FloorList.Add(new Vector2Int(x, y));
	}


	//Spawn any object
	GameObject Spawn(string obj, float x, float y)
	{
		return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.identity);
	}

	//Spawn any object rotated 90 degrees left
	GameObject SpawnRotateLeft(string obj, float x, float y)
	{
		return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.AngleAxis(-90, Vector3.forward));
	}

	//Spawn any object rotated 90 degrees right
	GameObject SpawnRotateRight(string obj, float x, float y)
	{
		return Instantiate(Prefabs[obj], new Vector3(x * GridSize, y * GridSize, 0.0f), Quaternion.AngleAxis(90, Vector3.forward));
	}
}
