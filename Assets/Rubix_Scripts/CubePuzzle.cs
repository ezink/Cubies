using UnityEngine;
using System.Threading;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Creates and Controls Rubics Cube of provided size wih provided images
/// </summary>
public class CubePuzzle : MonoBehaviour
{


#region "Public Variables"

	public ParticleSystem winParticles;

    public EDifficulty ShufflingDifficulty = EDifficulty.Easy;

    [Range(3, 10)]
    public int CubePuzzleSize = 3;

    [Range(0.1f, 2f)]
    public float CubePuzzleScale = 1f;

    [Range(15, 100)]
    public float RotationSpeed = 15f;

#endregion

#region "Private Variables"

    private GameObject[,,] _cubes; //Cubes Instances holder Variable
    private GameObject _cubeInstanceObject; //Main cube instance object
    private CubesScript[, ,] _cubesScriptInstances;


    private Vector3 _cubeCentrePoint = new Vector3();
	private GameObject[] cubies;
	public float CubeSolvedDelay;
	private bool CanSolveNow = false;
	public Animator EndAnims;
	public Animator HUDAnims;
	public Animator DFBabyAnim;
	public AudioSource DFBabyEnergySound;

	public AudioSource ExplosionAudio;
	public AudioSource SparkSounds;
	public ParticleSystem SparkFX;
	public GameObject ShakeRig;


    //Variables for rotation of lanes and cube
    private Transform _emptyObject;

    private Queue<SRotationData> _animationQueue = new Queue<SRotationData>();

    private SRotationData _currentRotationData = new SRotationData()
    {
        IsRotationComplete = true,

        AxisOfRotation = Vector3.zero,
        DirectionOfRotation = ERotationDirection.None,
        TypeOfRotation = ERotationType.CubeRotation,
        RotationPieceLocation = Vector3.zero
    };
    private float _currentAngle = 0;  //For stopping rotation at 90 degrees

    //Number of moves counter
    private int _currentNoOfMoves = 0;
    
    //Mouse positions for mouse gesture detection
    private string _selectedCubeName = "";
    private Vector2 _mouseStartPosition = Vector3.zero;
    private Vector2 _mouseStopPosition = Vector3.zero;

    private bool _isShuffling = false;

    //For getting current axis of Rubics cube with respect to starting Axis 1=x, 2=y, 3=z
    private Vector3 _currentCubeRotation = new Vector3(1, 2, 3);


    private const string _cubeNameOffset = "Cube";


#endregion




	public void Start (){

        //Get cube game object to form rubics cube from it
        _cubeInstanceObject = transform.FindChild("CubeInstance").gameObject;

        BuildCube();

        //Shuffle cube
        ShuffleRubicsCube(ShufflingDifficulty);


		cubies = GameObject.FindGameObjectsWithTag("Cube");


	}

    public void Update ()
    {
		CubeSolvedDelay -= Time.deltaTime;
		if (CubeSolvedDelay <= 0) {
			CanSolveNow = true;
			CubeSolvedDelay = 0;
		}

#region "Input Handling For Cube Or Part Of Cube Rotation"

        //Input for lanes selection
        if (Input.GetMouseButtonDown(0) && !_isShuffling)
        {
            _mouseStartPosition = Input.mousePosition;

            //Get ray collision cube name
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 100f))
            {
                if (hit.collider.transform.name.Contains(_cubeNameOffset))
                {
                    _selectedCubeName = hit.collider.transform.name;
                }
            }

        }
        else if (Input.GetMouseButtonUp(0) && !_isShuffling)
        {
            ERotationDirection rotDirection = ERotationDirection.None;

            _mouseStopPosition = Input.mousePosition;

            //Detect gesture
            if (Mathf.Abs(_mouseStartPosition.y - _mouseStopPosition.y) < (float)Screen.height * 0.1f)
            {
                
                if (_mouseStopPosition.x - _mouseStartPosition.x > (float)Screen.width * 0.2f)
                {
                    rotDirection = ERotationDirection.Right;
                }
                else if (_mouseStopPosition.x - _mouseStartPosition.x < -(float)Screen.width * 0.2f)
                {
                    rotDirection = ERotationDirection.Left;
                }

            }
            else if (Mathf.Abs(_mouseStartPosition.x - _mouseStopPosition.x) < (float)Screen.width * 0.1f)
            {
                if (_mouseStopPosition.y - _mouseStartPosition.y > (float)Screen.height * 0.2f)
                {
                    rotDirection = ERotationDirection.Up;
                }
                else if (_mouseStopPosition.y - _mouseStartPosition.y < -(float)Screen.height * 0.2f)
                {
                    rotDirection = ERotationDirection.Down;
                }
            }


            if (rotDirection != ERotationDirection.None)
            {

                if (_selectedCubeName == "")
                {
                    RotateVisualCube(rotDirection);
                }
                else
                {
                    //convert cube name to its index in cubes array
                    string numbersFromName = _selectedCubeName.Trim(_cubeNameOffset.ToCharArray()).TrimStart(new char[] { '_' });
                    string[] Nnumbers = numbersFromName.Split(new char[] { '_' });

                    Vector3 PiecePos = new Vector3(System.Convert.ToInt32(Nnumbers[2]) - 1,
                                            System.Convert.ToInt32(Nnumbers[0]) - 1,
                                            System.Convert.ToInt32(Nnumbers[1]) - 1);
                    RotateVisualLane(PiecePos, rotDirection);
                }

                _currentNoOfMoves++;

            }

            _selectedCubeName = "";  //Reset to cube rotation
        }


#endregion

#region "Rotation Animation Handling"

        if (_animationQueue.Count > 0 &&
                _currentRotationData.IsRotationComplete)
        {
            _currentRotationData.DirectionOfRotation = ERotationDirection.None;
            _currentRotationData = _animationQueue.Dequeue();
            _currentAngle = 0;

            ReleaseChilds();

            if (_currentRotationData.TypeOfRotation == ERotationType.CubeRotation)
            {
                GrabCube();
            }
            else if (_currentRotationData.TypeOfRotation == ERotationType.LaneRotation)
            {
                GrabLaneCubes(_currentRotationData.RotationPieceLocation, _currentRotationData.DirectionOfRotation);
            }
        }
        else if (_animationQueue.Count == 0 && _isShuffling)
            _isShuffling = false;


        if (!_currentRotationData.IsRotationComplete && 
                _currentRotationData.DirectionOfRotation != ERotationDirection.None)
        {
            _currentAngle += RotationSpeed * 10 * Time.deltaTime;
            _emptyObject.transform.Rotate(_currentRotationData.AxisOfRotation, RotationSpeed * 10 * Time.deltaTime);

            if (_currentAngle >= 90)
            {
                //If we have moved more then 90 move back
                _emptyObject.transform.Rotate(_currentRotationData.AxisOfRotation, 90 - _currentAngle);

                //Rotate cube inside array
                if (_currentRotationData.TypeOfRotation == ERotationType.CubeRotation)
                    RotateCubeInArray(_currentRotationData.DirectionOfRotation);
                else if (_currentRotationData.TypeOfRotation == ERotationType.LaneRotation)
                    RotateLaneInArray(_currentRotationData.RotationPieceLocation, _currentRotationData.DirectionOfRotation);

                //Check if rubics cube is solved
                if (IsRubicsCubeSolved())
                    OnRubicsCubeSolved();

                //Rotation complete
                _currentAngle = 0;

                _currentRotationData.IsRotationComplete = true;
                _currentRotationData.DirectionOfRotation = ERotationDirection.None;

            }
        }

#endregion

    }

    


    /// <summary>
    /// Grabs all the cubes inside cube for rotation
    /// </summary>
    private void GrabCube () 
    {
        //Reset rotation
        _emptyObject.transform.eulerAngles = Vector3.zero;

        _emptyObject.transform.position = _cubeCentrePoint;

        for (int Y = 0; Y < CubePuzzleSize; Y++)
            for (int Z = 0; Z < CubePuzzleSize; Z++)
                for (int X = 0; X < CubePuzzleSize; X++)
                    _cubes[Y, Z, X].transform.parent = _emptyObject;
    }

    /// <summary>
    /// Release captured child cubes of rubiks cube
    /// </summary>
    private void ReleaseChilds () 
    {
        for (int Y = 0; Y < CubePuzzleSize; Y++)
            for (int Z = 0; Z < CubePuzzleSize; Z++)
                for (int X = 0; X < CubePuzzleSize; X++)
                    _cubes[Y, Z, X].transform.parent = null;

        //Reset rotation
        _emptyObject.transform.eulerAngles = Vector3.zero;
    }

    /// <summary>
    /// Grab cubes from individual lane for rotation of that lane
    /// </summary>
    /// <param name="pieceLocation">Location of piece inside Rubiks cube instance array</param>
    /// <param name="directionOfRotation">Which direction to move thi slane</param>
    private void GrabLaneCubes (Vector3 pieceLocation, ERotationDirection directionOfRotation) 
    {

        if (pieceLocation.x < 0 || pieceLocation.x > CubePuzzleSize - 1 ||
             pieceLocation.y < 0 || pieceLocation.y > CubePuzzleSize - 1 ||
             pieceLocation.z < 0 || pieceLocation.z > CubePuzzleSize - 1)
        {
            return;
        }


        //Reset rotation
        _emptyObject.transform.eulerAngles = Vector3.zero;

        _emptyObject.transform.position = _cubeCentrePoint;


        //Grab according to direction of rotation
        if (directionOfRotation == ERotationDirection.Down || directionOfRotation == ERotationDirection.Up)
        {

            for (int Y = 0; Y < CubePuzzleSize; Y++)
                for (int Z = 0; Z < CubePuzzleSize; Z++)
                    _cubes[Y, Z, (int)pieceLocation.x].transform.parent = _emptyObject;
                
        }
        else if (directionOfRotation == ERotationDirection.Left || directionOfRotation == ERotationDirection.Right)
        {

            for (int X = 0; X < CubePuzzleSize; X++)
                for (int Z = 0; Z < CubePuzzleSize; Z++)
                    _cubes[(int)pieceLocation.y, Z, X].transform.parent = _emptyObject;

        }


    }


#region "Cube Rotation Methods For Rotating Cube In Display"

    private void RotateVisualCube (ERotationDirection directionOfRotation)
    {

        SRotationData TempRotData = new SRotationData()
        {
            DirectionOfRotation = directionOfRotation,
            IsRotationComplete = false,
            RotationPieceLocation = Vector3.zero,
            TypeOfRotation = ERotationType.CubeRotation,

            AxisOfRotation = Vector3.zero
        };


        float Temp = 0;

        switch (directionOfRotation)
        {

            case ERotationDirection.Left:
                //Change current axis with respect to origional axis
                Temp = _currentCubeRotation.x;
                _currentCubeRotation.x = _currentCubeRotation.z;
                _currentCubeRotation.z = Temp;

                TempRotData.AxisOfRotation.y = 1;
                break;


            case ERotationDirection.Right:
                //Change current axis with respect to origional axis
                Temp = _currentCubeRotation.x;
                _currentCubeRotation.x = _currentCubeRotation.z;
                _currentCubeRotation.z = Temp;

                TempRotData.AxisOfRotation.y = -1;
                break;



            case ERotationDirection.Up:
                //Change current axis with respect to origional axis
                Temp = _currentCubeRotation.y;
                _currentCubeRotation.y = _currentCubeRotation.z;
                _currentCubeRotation.z = Temp;

                TempRotData.AxisOfRotation.x = 1;
                break;


            case ERotationDirection.Down:
                //Change current axis with respect to origional axis
                Temp = _currentCubeRotation.y;
                _currentCubeRotation.y = _currentCubeRotation.z;
                _currentCubeRotation.z = Temp;

                TempRotData.AxisOfRotation.x = -1;
                break;


            case ERotationDirection.None:
                break;

        }


        _animationQueue.Enqueue(TempRotData);

    }

    private void RotateVisualLane (Vector3 pieceLocation, ERotationDirection directionOfRotation)
    {
        if (pieceLocation.x < 0 || pieceLocation.x > CubePuzzleSize - 1 ||
             pieceLocation.x < 0 || pieceLocation.x > CubePuzzleSize - 1 ||
             pieceLocation.x < 0 || pieceLocation.x > CubePuzzleSize - 1)
        {
            return;
        }
        
        SRotationData TempRotData = new SRotationData()
        {
            DirectionOfRotation = directionOfRotation,
            IsRotationComplete = false,
            RotationPieceLocation = pieceLocation,
            TypeOfRotation = ERotationType.LaneRotation,

            AxisOfRotation = Vector3.zero
        };

        switch (directionOfRotation)
        {
            case ERotationDirection.Left:
                TempRotData.AxisOfRotation.y = 1;
                break;

            case ERotationDirection.Right:
                TempRotData.AxisOfRotation.y = -1;
                break;

            case ERotationDirection.Up:
                TempRotData.AxisOfRotation.x = 1;
                break;

            case ERotationDirection.Down:
                TempRotData.AxisOfRotation.x = -1;
                break;

            case ERotationDirection.None:
                break;
        }


        _animationQueue.Enqueue(TempRotData);

    }

#endregion

#region "Cube Rotation Methods For Rotating Cube In Array"

    private void RotateCubeInArray (ERotationDirection directionOfRotation)
    {
        if (directionOfRotation == ERotationDirection.None)
            return;


        GameObject[, ,] TempCubes = new GameObject[CubePuzzleSize, CubePuzzleSize, CubePuzzleSize];

        lock (_cubes)
        {
            for (int Y = 0; Y < CubePuzzleSize; Y++)
                for (int X = 0; X < CubePuzzleSize; X++)
                    for (int Z = 0; Z < CubePuzzleSize; Z++)
                        TempCubes[Y, X, Z] = _cubes[Y, X, Z];


            for (int Y = 0; Y < CubePuzzleSize; Y++)
            {
                for (int Z = 0; Z < CubePuzzleSize; Z++)
                {
                    for (int X = 0; X < CubePuzzleSize; X++)
                    {

                        switch (directionOfRotation)
                        {
                            case ERotationDirection.Left:
                                _cubes[Y, Z, X] = TempCubes[Y, X, CubePuzzleSize - Z - 1];
                                goto default;


                            case ERotationDirection.Right:
                                _cubes[Y, Z, X] = TempCubes[Y, CubePuzzleSize - X - 1, Z];
                                goto default;


                            case ERotationDirection.Up:
                                _cubes[Y, Z, X] = TempCubes[Z, CubePuzzleSize -  Y - 1, X];
                                goto default;


                            case ERotationDirection.Down:
                                _cubes[Y, Z, X] = TempCubes[CubePuzzleSize - Z - 1, Y, X];
                                goto default;


                            default:
                                _cubes[Y, Z, X].name = _cubeNameOffset + "_" + (Y + 1).ToString() + "_" + (Z + 1).ToString() + "_" +
                                                                            (X + 1).ToString();
                                break;

                        }


                    }
                }
            }


        }

    }

    private void RotateLaneInArray (Vector3 pieceLocation, ERotationDirection directionOfRotation)
    {

        if (pieceLocation.x < 0 || pieceLocation.x > CubePuzzleSize - 1 ||
            pieceLocation.x < 0 || pieceLocation.x > CubePuzzleSize - 1 ||
            pieceLocation.x < 0 || pieceLocation.x > CubePuzzleSize - 1)
        {
            return;
        }

        lock (_cubes)
        {

#region "Left right rotation"

            if (directionOfRotation == ERotationDirection.Left || directionOfRotation == ERotationDirection.Right)
            {
                GameObject[,] TempCubes = new GameObject[CubePuzzleSize, CubePuzzleSize];

                //Get references from X and Z in tempcubes
                for (int Z = 0; Z < CubePuzzleSize; Z++)
                    for (int X = 0; X < CubePuzzleSize; X++)
                        TempCubes[Z, X] = _cubes[(int)pieceLocation.y, Z, X];


                for (int Z = 0; Z < CubePuzzleSize; Z++)
                {
                    for (int X = 0; X < CubePuzzleSize; X++)
                    {

                        switch (directionOfRotation)
                        {
                            case ERotationDirection.Left:
                                _cubes[(int)pieceLocation.y, Z, X] = TempCubes[X, CubePuzzleSize - Z - 1];
                                goto default;

                            case ERotationDirection.Right:
                                _cubes[(int)pieceLocation.y, Z, X] = TempCubes[CubePuzzleSize - X - 1, Z];
                                goto default;

                            default:
                                _cubes[(int)pieceLocation.y, Z, X].name = _cubeNameOffset + "_" + ((int)pieceLocation.y + 1).ToString() + "_" + (Z + 1).ToString() + "_" +
                                                                            (X + 1).ToString();
                                break;
                        }

                    }
                }


            }

#endregion

#region "Top Down Rotation"

            else if (directionOfRotation == ERotationDirection.Up || directionOfRotation == ERotationDirection.Down)
            {
                GameObject[,] TempCubes = new GameObject[CubePuzzleSize, CubePuzzleSize];

                //Get references from X and Z in tempcubes
                for (int Y = 0; Y < CubePuzzleSize; Y++)
                    for (int Z = 0; Z < CubePuzzleSize; Z++)
                        TempCubes[Y, Z] = _cubes[Y, Z, (int)pieceLocation.x];


                for (int Y = 0; Y < CubePuzzleSize; Y++)
                {
                    for (int Z = 0; Z < CubePuzzleSize; Z++)
                    {

                        switch (directionOfRotation)
                        {
                            case ERotationDirection.Up:
                                _cubes[Y, Z, (int)pieceLocation.x] = TempCubes[Z, CubePuzzleSize -  Y - 1];
                                goto default;

                            case ERotationDirection.Down:
                                _cubes[Y, Z, (int)pieceLocation.x] = TempCubes[CubePuzzleSize - Z - 1, Y];
                                goto default;

                            default:
                                _cubes[Y, Z, (int)pieceLocation.x].name = _cubeNameOffset + "_" + (Y + 1).ToString() + "_" + (Z + 1).ToString() + "_" +
                                                                            ((int)pieceLocation.x + 1).ToString();
                                break;
                        }

                    }
                }


            }

#endregion


        }


    }

#endregion

#region "Helper Methods"

    private bool IsRubicsCubeSolved ()
    {
        if (_isShuffling)
            return false;

        for (int Y = 0; Y < CubePuzzleSize; Y++)
        {
            for (int Z = 0; Z < CubePuzzleSize; Z++)
            {
                for (int X = 0; X < CubePuzzleSize; X++)
                {
                    CubesScript Temp = _cubes[Y, Z, X].GetComponent<CubesScript>();

                    if (_cubesScriptInstances[Y, Z, X]._thisCubeLocation.x != Temp._thisCubeLocation.x ||
                         _cubesScriptInstances[Y, Z, X]._thisCubeLocation.x != Temp._thisCubeLocation.x ||
                        _cubesScriptInstances[Y, Z, X]._thisCubeLocation.x != Temp._thisCubeLocation.x)
                        return false;
                }
            }
        }

        return true;
    }

    private void BuildCube ()
    {
        //Scale cube instance
        _cubeInstanceObject.transform.localScale = new Vector3(CubePuzzleScale/15, CubePuzzleScale/15, CubePuzzleScale/15);

        //Gets cube size
        Vector3 CubeSize = _cubeInstanceObject.GetComponent<Renderer>().bounds.size;

        //Gap between cubes
        Vector3 GapBetweenCubes = new Vector3(CubeSize.x * 0.03f, CubeSize.y * 0.03f, CubeSize.z * 0.03f);


        //Initialize cubes instance variable
        _cubes = new GameObject[CubePuzzleSize, CubePuzzleSize, CubePuzzleSize];
        _cubesScriptInstances = new CubesScript[CubePuzzleSize, CubePuzzleSize, CubePuzzleSize];


        //Place cubes inside array appropriately and position them in world
        for (int Y = 0; Y < CubePuzzleSize; Y++)
        {
            for (int Z = 0; Z < CubePuzzleSize; Z++)
            {
                for (int X = 0; X < CubePuzzleSize; X++)
                {

                    _cubes[Y, Z, X] = GameObject.Instantiate(_cubeInstanceObject) as GameObject;

                    //Give it a name
                    _cubes[Y, Z, X].name = _cubeNameOffset + "_" + (Y + 1).ToString() + "_" + (Z + 1).ToString() + "_" + (X + 1).ToString();

                    //Enable this cube
                    _cubes[Y, Z, X].SetActive(true);

                    //Move it to its position in rubics cube starting from (0,0,0);
                    _cubes[Y, Z, X].transform.position = new Vector3((GapBetweenCubes.x * X) + (CubeSize.x * X),
                             (GapBetweenCubes.y * Y) + (CubeSize.y * Y), (GapBetweenCubes.z * Z) + (CubeSize.z * Z));

                    //Get cubes script instances
                    _cubesScriptInstances[Y, Z, X] = _cubes[Y, Z, X].GetComponent<CubesScript>();

                    //Set cubes origional positions in Rubics cube
                    _cubesScriptInstances[Y, Z, X]._thisCubeLocation = new Vector3(Y, Z, X);

                }
            }
        }


        //Calculate cube centre point
        _cubeCentrePoint.x = (_cubes[0, 0, 0].transform.position.x +
            (_cubes[0, 0, CubePuzzleSize - 1].transform.position.x - _cubes[0, 0, 0].transform.position.x)) / 2;
        _cubeCentrePoint.y = (_cubes[0, 0, 0].transform.position.y +
            (_cubes[CubePuzzleSize - 1, 0, 0].transform.position.y - _cubes[0, 0, 0].transform.position.y)) / 2;
        _cubeCentrePoint.z = (_cubes[0, 0, 0].transform.position.z +
            (_cubes[0, CubePuzzleSize - 1, 0].transform.position.z - _cubes[0, 0, 0].transform.position.z)) / 2;


        //Get empty object for rotation
        _emptyObject = gameObject.transform.FindChild("Empty Object");

        //Texture all cubes appropriately
       // TextureCubes();
    }

    private void DestroyCube ()
    {
        //Destroy Cubes
        for (int Y = 0; Y < CubePuzzleSize; Y++)
            for (int Z = 0; Z < CubePuzzleSize; Z++)
                for (int X = 0; X < CubePuzzleSize; X++)
                    Destroy(_cubes[Y, Z, X].transform.gameObject);

    }

#endregion

    
    public void OnRubicsCubeSolved ()
    {	

		if (CanSolveNow) {
			PerlinShake p = ShakeRig.GetComponent<PerlinShake> () as PerlinShake;
			if (p)
				p.PlayShake();

			HUDAnims.SetTrigger("FadeOutHUD");


			Debug.LogWarning ("Rubics cube solved");
			for (int i = 0; i < cubies.Length; i++) {
				Destroy (cubies [i]);
			}

			//End Explosion FX
			winParticles.Play ();
			CanSolveNow = true;
			ExplosionAudio.Play();
			DFBabyAnim.SetTrigger("PlayBaby");
			DFBabyEnergySound.Play();

			//EndSign
			EndAnims.SetTrigger("TheEndBegin");

			//SparkFX
			SparkSounds.Play();
			SparkFX.Play();
		}
    }

    /// <summary>
    /// Destroy previous cube and recreates the cube
    /// </summary>
    public void RebuildCube ()
    {
        if (_isShuffling)
            return;

        DestroyCube();
        BuildCube();
    }

    public void ShuffleRubicsCube (EDifficulty ShuffleDifficulty)
    {
        _isShuffling = true;
        int ShuffleTimes = 0;

        Random.seed = System.DateTime.Now.Second;

        switch (ShuffleDifficulty)
        {
            case EDifficulty.Easy:
                ShuffleTimes = Random.Range(1, 1);
                break;

            case EDifficulty.Medium:
                ShuffleTimes = Random.Range(7, 14);
                break;

            case EDifficulty.Difficult:
                ShuffleTimes = Random.Range(32, 45);
                break;
        }


        for (int ST = 0; ST < ShuffleTimes; ST++)
        {
            ERotationType TempRT = (ERotationType)Random.Range(0, 2);
            ERotationDirection TempRD = (ERotationDirection)Random.Range(0, 4);

            if (TempRT == ERotationType.CubeRotation)
            {
                RotateVisualCube(TempRD);
            }
            else if (TempRT == ERotationType.LaneRotation)
            {
                //RotateVisualLane(new Vector3((int)Random.Range(0, RubicsCubeSize), 
                //0, (int)Random.Range(0, RubicsCubeSize)), TempRD);
                RotateVisualLane(new Vector3(0, 0, 0), TempRD);
            }

        }

        
    }

    public void ResetRubicsCube ()
    {
        if (_isShuffling)
            return;

        DestroyCube();

        BuildCube();

        ShuffleRubicsCube(ShufflingDifficulty);
    }

}


public enum ERotationType
{
    CubeRotation = 0,
    LaneRotation = 1
}

public enum ERotationDirection
{
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3,
    None = 20
}

public enum EDifficulty
{
    Easy = 0,
    Medium = 1,
    Difficult = 2
}

public struct SRotationData
{
    public bool IsRotationComplete;
    public Vector3 AxisOfRotation;

    public ERotationType TypeOfRotation;

    public ERotationDirection DirectionOfRotation;

    public Vector3 RotationPieceLocation; //location of lane to move in cubes array

    public override string ToString()
    {
        string Result = "";
        Result = Result + "Rotation Data" + System.Environment.NewLine;

        Result = Result + " IsRotationComplete : " + IsRotationComplete + System.Environment.NewLine;
        Result = Result + " AxisOfRotation : " + AxisOfRotation + System.Environment.NewLine;
        Result = Result + " TypeOfRotation : " + TypeOfRotation + System.Environment.NewLine;
        Result = Result + " DirectionOfRotation : " + DirectionOfRotation + System.Environment.NewLine;
        Result = Result + " RotationPieceLocation : " + RotationPieceLocation;


        return Result;
    }

}