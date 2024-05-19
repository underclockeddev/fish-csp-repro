using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;
namespace FishCspMvp
{
    public class OnlineEngine : NetworkBehaviour
    {
        bool cachedThrottle;
        float cachedTurn;
        public PredictionRigidbody2D PredictionRigidbody2d;

        void Awake()
        {
            PredictionRigidbody2d = ObjectCaches<PredictionRigidbody2D>.Retrieve();
            PredictionRigidbody2d.Initialize(GetComponent<Rigidbody2D>());
        }

        void Update()
        {
            cachedThrottle = Input.GetKey(KeyCode.W);
            cachedTurn = Input.GetKey(KeyCode.A) ? -1 : Input.GetKey(KeyCode.D) ? 1 : 0 ;
        }

        void OnDestroy()
        {
            ObjectCaches<PredictionRigidbody2D>.StoreAndDefault(ref PredictionRigidbody2d);
        }

        void TimeManager_OnPostTick()
        {
            CreateReconcile();
        }

        public override void CreateReconcile()
        {
            ReconcileData rd = new ReconcileData(PredictionRigidbody2d);
            ReconcileState(rd);
        }

        [Reconcile]
        void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            PredictionRigidbody2d.Reconcile(data.PredictionRigidbody2d);
        }

        public override void OnStartNetwork()
        {
            TimeManager.OnTick += TimeManager_OnTick;
            TimeManager.OnPostTick += TimeManager_OnPostTick;
        }
        void TimeManager_OnTick()
        {
            RunInputs(CreateReplicateData());
        }
        [Replicate]
        void RunInputs(ReplicationData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            PredictionRigidbody2d.AddForce(PredictionRigidbody2d.Rigidbody2D.transform.up * (data.Throttle ? 1 : 0));
            PredictionRigidbody2d.AddForce(PredictionRigidbody2d.Rigidbody2D.transform.right * data.Turn);

            if (IsServerInitialized)
            {
                UnityEngine.Debug.Log($"I am the server, on tick {NetworkManager.TimeManager.Tick} processing physics tick {data.GetTick()}\r\n" +
                    $"The current state is {state}\r\n" +
                    $"I am applying {PredictionRigidbody2d.Rigidbody2D.transform.up * (data.Throttle ? 1 : 0)} force for forward/backward\r\n" +
                    $"I am applying {PredictionRigidbody2d.Rigidbody2D.transform.right * data.Turn} force for turning", gameObject);
            }


            if (IsClientInitialized)
            {
                UnityEngine.Debug.Log($"I am the client, on tick {NetworkManager.TimeManager.Tick} processing physics tick {data.GetTick()}\r\n" +
                    $"The current state is {state}\r\n" +
                    $"I am applying {PredictionRigidbody2d.Rigidbody2D.transform.up * (data.Throttle ? 1 : 0)} force for forward/backward\r\n" +
                    $"I am applying {PredictionRigidbody2d.Rigidbody2D.transform.right * data.Turn} force for turning", gameObject);
            }
            
            PredictionRigidbody2d.Simulate();
        }

        ReplicationData CreateReplicateData()
        {
            if (!IsOwner)
                return default(ReplicationData);

            ReplicationData md = new ReplicationData(cachedThrottle, cachedTurn);
            cachedThrottle = false;
            cachedTurn = 0;

            return md;
        }

        public override void OnStopNetwork()
        {
            TimeManager.OnTick -= TimeManager_OnTick;
            TimeManager.OnPostTick -= TimeManager_OnPostTick;
        }
    }
    public struct ReplicationData : IReplicateData
    {
        public bool Throttle;
        public float Turn;

        uint _tick;
        public ReplicationData(bool _throttle, float _turn) : this()
        {
                Throttle = _throttle;
                Turn = _turn;
        }
        public void Dispose()
        { }
        public uint GetTick()
        {
            return _tick;
        }
        public void SetTick(uint value)
        {
            _tick = value;
        }
    }
    public struct ReconcileData : IReconcileData
    {
        public PredictionRigidbody2D PredictionRigidbody2d;

        uint _tick;

        public ReconcileData(PredictionRigidbody2D pr) : this()
        {
            PredictionRigidbody2d = pr;
        }
        public void Dispose()
        { }
        public uint GetTick()
        {
            return _tick;
        }
        public void SetTick(uint value)
        {
            _tick = value;
        }
    }
}