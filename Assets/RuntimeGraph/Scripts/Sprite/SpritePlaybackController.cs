using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace RuntimeGraph.Sprite
{
    /// <summary>
    /// Playback controller for managing tempo, beat timing, and traveler coordination
    /// </summary>
    public class SpritePlaybackController : MonoBehaviour
    {
        [System.Serializable]
        public class PlaybackSettings
        {
            [Range(60f, 200f)]
            public float tempo = 120f; // BPM (beats per minute)
            
            [Range(0.25f, 4f)]
            public float beatDivision = 1f; // Multiplier for beat speed (0.5 = half speed, 2 = double speed)
            
            public bool isPlaying = false;
            public bool loop = true;
            public float masterVolume = 1f;
        }
        
        [Header("Playback Settings")]
        public PlaybackSettings settings = new PlaybackSettings();
        
        [Header("Timing")]
        public bool useMetronome = false;
        public AudioSource metronomeSource;
        public AudioClip metronomeClick;
        
        private SpriteRuntimeGraph graph;
        private List<SpriteTraveler> activeTravelers = new List<SpriteTraveler>();
        private HashSet<string> startNodesWithActiveTravelers = new HashSet<string>();
        private float lastBeatTime = 0f;
        private int currentBeat = 0;
        private bool isInitialized = false;
        
        // Timing calculations
        public float QuarterNoteInterval => 60f / settings.tempo; // seconds per quarter note
        public float CurrentBeatInterval => QuarterNoteInterval / settings.beatDivision;
        public float CurrentTempoMultiplier => settings.beatDivision * (settings.tempo / 120f);
        
        // Events
        public System.Action<int> OnBeat;
        public System.Action OnPlaybackStarted;
        public System.Action OnPlaybackStopped;
        
        public void Initialize(SpriteRuntimeGraph graph)
        {
            this.graph = graph;
            
            // Setup metronome if needed
            if (useMetronome && metronomeSource == null)
            {
                SetupMetronome();
            }
            
            isInitialized = true;
        }
        
        private void SetupMetronome()
        {
            // Create metronome audio source
            var metronomeGO = new GameObject("Metronome");
            metronomeGO.transform.SetParent(transform);
            
            metronomeSource = metronomeGO.AddComponent<AudioSource>();
            metronomeSource.playOnAwake = false;
            metronomeSource.volume = 0.3f;
            metronomeSource.pitch = 1f;
            
            // Create simple click sound if none provided
            if (metronomeClick == null)
            {
                metronomeClick = CreateMetronomeClick();
            }
        }
        
        private AudioClip CreateMetronomeClick()
        {
            // Create a simple sine wave click
            int sampleRate = 44100;
            float duration = 0.1f;
            int samples = Mathf.RoundToInt(sampleRate * duration);
            float[] audioData = new float[samples];
            
            float frequency = 800f; // Click frequency
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Exp(-t * 20f); // Quick decay
                audioData[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * 0.3f;
            }
            
            AudioClip clip = AudioClip.Create("MetronomeClick", samples, 1, sampleRate, false);
            clip.SetData(audioData, 0);
            return clip;
        }
        
        private void Update()
        {
            if (!isInitialized || !settings.isPlaying) return;
            
            UpdateTiming();
            UpdateTravelers();
        }
        
        private void UpdateTiming()
        {
            float currentTime = Time.time;
            float timeSinceLastBeat = currentTime - lastBeatTime;
            
            if (timeSinceLastBeat >= CurrentBeatInterval)
            {
                ProcessBeat();
                lastBeatTime = currentTime;
                currentBeat++;
            }
        }
        
        private void ProcessBeat()
        {
            // Trigger beat event
            OnBeat?.Invoke(currentBeat);
            
            // Play metronome
            if (useMetronome && metronomeSource != null && metronomeClick != null)
            {
                metronomeSource.PlayOneShot(metronomeClick, settings.masterVolume);
            }
            
            // Process node activations and traveler spawning
            ProcessNodeActivations();
        }
        
        private void ProcessNodeActivations()
        {
            if (graph == null) return;
            
            // Check all nodes for activation conditions
            foreach (var nodeData in graph.Nodes)
            {
                var node = graph.GetNode(nodeData.id);
                if (node == null || nodeData.mute) continue;
                
                // Only spawn travelers from nodes marked as IsStart
                if (!nodeData.isStart) continue;
                
                // Skip if this start node already has an active traveler
                if (startNodesWithActiveTravelers.Contains(nodeData.id)) continue;
                
                // Check probability
                if (Random.Range(0f, 100f) > nodeData.probability) continue;
                
                // Check if node should be activated (simplified - could be enhanced with more complex timing)
                bool shouldActivate = ShouldActivateNode(nodeData);
                
                if (shouldActivate)
                {
                    ActivateNode(node, nodeData);
                }
            }
        }
        
        private bool ShouldActivateNode(SpriteNode.NodeData nodeData)
        {
            // Simple activation logic - could be enhanced based on requirements
            // For now, activate nodes randomly or based on specific conditions
            
            if (nodeData.isLogicPoint)
            {
                return EvaluateLogicNode(nodeData);
            }
            
            // Start nodes should always activate on the first beat to ensure immediate traveler spawning
            if (nodeData.isStart && currentBeat == 0)
            {
                return true;
            }
            
            // Regular nodes activate based on timing or existing travelers
            return HasTravelersAtNode(nodeData.id) || (currentBeat % 4 == 0 && Random.Range(0f, 1f) < 0.3f);
        }
        
        private bool EvaluateLogicNode(SpriteNode.NodeData nodeData)
        {
            // Implement logic node evaluation (AND/OR/NOT/Toggle)
            switch (nodeData.logicType)
            {
                case SpriteNode.LogicType.AND:
                    return EvaluateANDLogic(nodeData);
                case SpriteNode.LogicType.OR:
                    return EvaluateORLogic(nodeData);
                case SpriteNode.LogicType.NOT:
                    return EvaluateNOTLogic(nodeData);
                case SpriteNode.LogicType.Toggle:
                    return EvaluateToggleLogic(nodeData);
                default:
                    return false;
            }
        }
        
        private bool EvaluateANDLogic(SpriteNode.NodeData nodeData)
        {
            // Check if all input conditions are met
            return GetActiveInputCount(nodeData.id) >= GetTotalInputCount(nodeData.id);
        }
        
        private bool EvaluateORLogic(SpriteNode.NodeData nodeData)
        {
            // Check if at least one input condition is met
            return GetActiveInputCount(nodeData.id) > 0;
        }
        
        private bool EvaluateNOTLogic(SpriteNode.NodeData nodeData)
        {
            // Invert the input condition
            return GetActiveInputCount(nodeData.id) == 0;
        }
        
        private bool EvaluateToggleLogic(SpriteNode.NodeData nodeData)
        {
            // Toggle state on input
            // This would need state tracking - simplified for now
            return GetActiveInputCount(nodeData.id) > 0;
        }
        
        private void ActivateNode(SpriteNode node, SpriteNode.NodeData nodeData)
        {
            // Spawn travelers based on path behavior
            switch (nodeData.pathBehavior)
            {
                case SpriteNode.PathBehavior.Sequential:
                    SpawnSequentialTraveler(nodeData);
                    break;
                case SpriteNode.PathBehavior.WeightedRandom:
                    SpawnWeightedRandomTraveler(nodeData);
                    break;
                case SpriteNode.PathBehavior.Split:
                    SpawnSplitTravelers(nodeData);
                    break;
                case SpriteNode.PathBehavior.Instant:
                    ExecuteInstantTeleport(nodeData);
                    break;
            }
        }
        
        private float CalculateDistanceBasedTravelTime(SpriteConnection.ConnectionData connectionData)
        {
            if (connectionData == null || graph == null) return QuarterNoteInterval; // Default fallback
            
            var connectionInstance = graph.GetConnectionInstance(connectionData.id);
            if (connectionInstance != null)
            {
                return graph.CalculateTravelTimeFromDistance(connectionInstance);
            }
            
            return QuarterNoteInterval; // Fallback to 1 quarter note
        }
        
        private void SpawnSequentialTraveler(SpriteNode.NodeData nodeData)
        {
            var outgoingConnections = GetOutgoingConnections(nodeData.id);
            if (outgoingConnections.Count == 0) return;
            
            // Sort by creation order
            outgoingConnections.Sort((a, b) => a.creationOrder.CompareTo(b.creationOrder));
            
            // Use first connection (sequential behavior)
            var connection = outgoingConnections[0];
            float travelTime = CalculateDistanceBasedTravelTime(connection);
            SpawnTraveler(nodeData.id, connection.toNodeId, travelTime);
        }
        
        private void SpawnWeightedRandomTraveler(SpriteNode.NodeData nodeData)
        {
            var outgoingConnections = GetOutgoingConnections(nodeData.id);
            if (outgoingConnections.Count == 0) return;
            
            // Calculate total weight
            float totalWeight = 0f;
            foreach (var conn in outgoingConnections)
            {
                totalWeight += conn.weight;
            }
            
            // Select connection based on weight
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var connection in outgoingConnections)
            {
                currentWeight += connection.weight;
                if (randomValue <= currentWeight)
                {
                    float travelTime = CalculateDistanceBasedTravelTime(connection);
                    SpawnTraveler(nodeData.id, connection.toNodeId, travelTime);
                    break;
                }
            }
        }
        
        private void SpawnSplitTravelers(SpriteNode.NodeData nodeData)
        {
            var outgoingConnections = GetOutgoingConnections(nodeData.id);
            
            // Spawn traveler for each outgoing connection
            foreach (var connection in outgoingConnections)
            {
                float travelTime = CalculateDistanceBasedTravelTime(connection);
                SpawnTraveler(nodeData.id, connection.toNodeId, travelTime);
            }
        }
        
        private void ExecuteInstantTeleport(SpriteNode.NodeData nodeData)
        {
            var outgoingConnections = GetOutgoingConnections(nodeData.id);
            
            // Instantly trigger target nodes (no travel time)
            foreach (var connection in outgoingConnections)
            {
                var targetNode = graph.GetNode(connection.toNodeId);
                if (targetNode != null)
                {
                    var targetNodeData = targetNode.NodeDataInstance;
                    ActivateNode(targetNode, targetNodeData);
                }
            }
        }
        
        private void SpawnTraveler(string fromNodeId, string toNodeId, float travelTime)
        {
            var travelerData = new SpriteTraveler.TravelerData
            {
                id = System.Guid.NewGuid().ToString("N"),
                currentNodeId = fromNodeId,
                travelTime = travelTime,
                color = Color.yellow,
                size = 1f,
                isActive = true
            };
            
            // Check if this is spawning from a start node and track it
            var fromNode = graph.GetNode(fromNodeId);
            if (fromNode != null && fromNode.NodeDataInstance.isStart)
            {
                travelerData.originStartNodeId = fromNodeId;
                startNodesWithActiveTravelers.Add(fromNodeId);
            }
            
            var travelerGO = new GameObject($"Traveler_{travelerData.id}");
            travelerGO.transform.SetParent(transform);
            
            var traveler = travelerGO.AddComponent<SpriteTraveler>();
            traveler.Initialize(graph, travelerData);
            
            activeTravelers.Add(traveler);
            
            // Find the connection between fromNode and toNode to pass path information
            var connectionData = graph?.Connections?.Find(c => c.fromNodeId == fromNodeId && c.toNodeId == toNodeId);
            if (connectionData != null)
            {
                var connectionInstance = graph.GetConnectionInstance(connectionData.id);
                if (connectionInstance != null)
                {
                    // Use connection path for movement
                    traveler.StartMovementWithConnection(toNodeId, travelTime, connectionInstance);
                }
                else
                {
                    // Fallback to direct movement
                    traveler.StartMovement(toNodeId, travelTime);
                }
            }
            else
            {
                // Fallback to direct movement
                traveler.StartMovement(toNodeId, travelTime);
            }
        }
        
        private void UpdateTravelers()
        {
            // Clean up completed travelers
            for (int i = activeTravelers.Count - 1; i >= 0; i--)
            {
                var traveler = activeTravelers[i];
                if (traveler == null || !traveler.IsActive)
                {
                    // Remove start node from tracking if this traveler originated from one
                    if (traveler != null && !string.IsNullOrEmpty(traveler.TravelerDataInstance?.originStartNodeId))
                    {
                        startNodesWithActiveTravelers.Remove(traveler.TravelerDataInstance.originStartNodeId);
                    }
                    
                    activeTravelers.RemoveAt(i);
                    if (traveler != null)
                    {
                        DestroyImmediate(traveler.gameObject);
                    }
                }
            }
        }
        
        public void StartPlayback()
        {
            settings.isPlaying = true;
            lastBeatTime = Time.time;
            currentBeat = 0;
            
            // Clear any leftover state from previous playback
            startNodesWithActiveTravelers.Clear();
            
            // Immediately process the first beat to start travelers without delay
            ProcessBeat();
            
            OnPlaybackStarted?.Invoke();
        }
        
        public void StopPlayback()
        {
            settings.isPlaying = false;
            
            // Stop and destroy all travelers immediately
            for (int i = activeTravelers.Count - 1; i >= 0; i--)
            {
                var traveler = activeTravelers[i];
                if (traveler != null)
                {
                    traveler.StopMovement();
                    DestroyImmediate(traveler.gameObject);
                }
            }
            
            // Clear all tracking state
            activeTravelers.Clear();
            startNodesWithActiveTravelers.Clear();
            
            OnPlaybackStopped?.Invoke();
        }
        
        public void TogglePlayback()
        {
            if (settings.isPlaying)
                StopPlayback();
            else
                StartPlayback();
        }
        
        public void SetTempo(float newTempo)
        {
            settings.tempo = Mathf.Clamp(newTempo, 60f, 200f);
        }
        
        public void SetBeatDivision(float newDivision)
        {
            settings.beatDivision = Mathf.Clamp(newDivision, 0.25f, 4f);
        }
        
        // Helper methods
        private List<SpriteConnection.ConnectionData> GetOutgoingConnections(string nodeId)
        {
            var result = new List<SpriteConnection.ConnectionData>();
            if (graph?.Connections == null) return result;
            
            foreach (var conn in graph.Connections)
            {
                if (conn.fromNodeId == nodeId)
                {
                    result.Add(conn);
                }
            }
            
            return result;
        }
        
        private bool HasTravelersAtNode(string nodeId)
        {
            foreach (var traveler in activeTravelers)
            {
                if (traveler?.TravelerDataInstance?.currentNodeId == nodeId)
                {
                    return true;
                }
            }
            return false;
        }
        
        private int GetActiveInputCount(string nodeId)
        {
            // Count active inputs (travelers or recent activations)
            return HasTravelersAtNode(nodeId) ? 1 : 0;
        }
        
        private int GetTotalInputCount(string nodeId)
        {
            int count = 0;
            if (graph?.Connections == null) return count;
            
            foreach (var conn in graph.Connections)
            {
                if (conn.toNodeId == nodeId)
                {
                    count++;
                }
            }
            
            return count;
        }
    }
}