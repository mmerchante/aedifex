using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.PostProcessing;


/// <summary>
/// The director will consume emotion events and choose the specific camera cut/blends and behaviours.
/// Note that it cannot precompute all shots because the procedural scene may change.
/// </summary>
public class ProceduralCameraDirector : MonoBehaviorSingleton<ProceduralCameraDirector>, ProceduralEventListener
{
    public enum TransitionType
    {
        Cut = 0,
        Blend = 1,
        // TODO: Fades, swipes, etc
    }

    public struct CutRange
    {
        public float minCutTime;
        public float maxCutTime;
    }

    public struct ShotInformation
    {
        // Is it complete?
        public bool valid;

        // The duration of this potential shot
        // TODO: think about using CutRange instead, assume the maxCut as ending but let the director cut before
        public float duration;

        // In current+duration time (+- some margin), this event is the one that will be used for the next shot
        public EmotionEvent selectedNextEventTrigger;
        
        // Straight cut or blend, etc
        public TransitionType type;

        // The specific camera that will be used
        public ProceduralCamera selectedCamera;

        // The camera behaviour, which knows about interest points etc.
        public ProceduralCameraStrategy strategy;
    }
    
    private ProceduralCamera currentCamera;

    private ShotInformation currentShot;
    private ShotInformation nextShot;

    private float currentCutTime = 0f;
    
    private EmotionEngine emotionEngine;

    // The list of shots already made -- useful for reusing ideas to build coherency
    private List<ShotInformation> history = new List<ShotInformation>();

    private InterestPointGrid grid;
    private List<InterestPoint> interestPoints = new List<InterestPoint>();

    public void InitializeDirector(EmotionEngine engine)
    {
        this.emotionEngine = engine;
        this.currentShot = new ShotInformation();
        this.currentShot.valid = false;
        this.nextShot = new ShotInformation();
        this.grid = GetComponent<InterestPointGrid>();

        ProceduralEngine.Instance.EventDispatcher.AddListener(this);
    }

    protected void StartTransition(ShotInformation cut)
    {
        if (currentShot.valid)
            currentShot.strategy.StopStrategy();

        if(cut.valid)
        {
            this.history.Add(cut);
            this.currentShot = cut;
            this.currentCutTime = 0f;
            this.nextShot = new ShotInformation();
            this.currentCamera = cut.selectedCamera;
            this.currentCamera.InitializeCamera(currentShot.strategy, GetComponent<PostProcessingBehaviour>().profile);
            this.currentShot.strategy.StartStrategy();
        }
    }

    public InterestPointGrid GetGrid()
    {
        return grid;
    }

    public void InitializeGrid()
    {
        List<InterestPoint> toRemove = new List<InterestPoint>();

        // After the level was loaded
        foreach (InterestPoint p in interestPoints)
        {
            if (grid.ContainsPoint(p.transform.position))
                grid.AddInterestPoint(p);
            else
                toRemove.Add(p);
        }
        
        foreach (InterestPoint p in toRemove)
            interestPoints.Remove(p);
    }

    public void RegisterInterestPoint(InterestPoint p)
    {
        this.interestPoints.Add(p);
    }

    public void DeregisterInterestPoint(InterestPoint p)
    {
        this.interestPoints.Remove(p);
        this.grid.RemoveInterestPoint(p);
    }

    public List<InterestPoint> GetInterestPoints()
    {
        return interestPoints;
    }

    // For now, we iterate over all interest points, because we don't have many.
    // In the future, implement this in the grid (ref: Dynamic Scene Visibility Culling using a Regular Grid)
    public List<InterestPoint> GetInterestPointsOnFrustum(Matrix4x4 viewProj, out float accumulatedImportance)
    {
        List<InterestPoint> result = new List<InterestPoint>();
        accumulatedImportance = 0f;

        foreach (InterestPoint p in interestPoints)
        {
            Vector4 ndc = viewProj * new Vector4(p.transform.position.x, p.transform.position.y, p.transform.position.z, 1f);
            ndc /= ndc.w;
            ndc.x = ndc.x * .5f + .5f;
            ndc.y = ndc.y * .5f + .5f;

            if (ndc.x >= 0f && ndc.x <= 1f && ndc.y >= 0f && ndc.y <= 1f && ndc.z >= 0f && ndc.z <= 1f)
            {
                result.Add(p);
                accumulatedImportance += p.importance;
            }
        }

        return result;
    }

    public InterestPoint SampleInterestPoint()
    {
        float sum = interestPoints.Sum(x => x.EvaluateHeuristic(true));
        float value = (float)ProceduralEngine.Instance.RNG.NextDouble() * sum;

        foreach (InterestPoint ip in interestPoints)
        {
            value -= ip.EvaluateHeuristic(true);

            if (value <= 0f)
                return ip;
        }

        return null;
    }

    protected float GetEventPriority(EmotionEvent e)
    {
        float p = e.intensity;

        if (e.type == EmotionEvent.EmotionEventType.LocalMaximum || e.type == EmotionEvent.EmotionEventType.LocalMinimum)
            p *= 2f;

        // Some bias towards start
        if (e.type == EmotionEvent.EmotionEventType.Start)
            p *= 1.25f;

        if (e.chunkDelimitsSegment)
            p *= 1.75f;

        // Even if this is considered in intensity, we want to bias these cases
        if (e.harmonicDifference > 0)
            p *= 1 + e.harmonicDifference * .5f;

        // TODO: check if the track is rythm or melody, etc!!
        
        return p;
    }

    protected ProceduralCamera InstanceCamera()
    {
        GameObject go = new GameObject("ShotCamera-" + history.Count);
        return go.AddComponent<ProceduralCamera>();
    }

    protected ProceduralCameraStrategy TryFindStrategy(EmotionEvent e, float shotDuration)
    {
        int samples = 32;
        InterestPoint point = FindInterestPoint();
        List<KeyValuePair<ProceduralCameraStrategy, float>> strategies = new List<KeyValuePair<ProceduralCameraStrategy, float>>();

        float totalWeight = 0f;
        
        for(int i = 0; i < samples; ++i)
        {
            ProceduralCameraStrategy s = new DollyCameraStrategy();

            // If the strategy failed finding a proposal, ignore it
            if (!s.Propose(e, point, shotDuration))
                continue;

            Matrix4x4 viewProj = s.GetViewProjection();

            float frustumWeightAccum = 0f;
            List<InterestPoint> frustumPoints = GetInterestPointsOnFrustum(viewProj, out frustumWeightAccum);

            float weight = s.Evaluate(e, frustumPoints, frustumWeightAccum);
            totalWeight += weight;

            strategies.Add(new KeyValuePair<ProceduralCameraStrategy, float>(s, weight));
        }
                
        return ProceduralEngine.SelectRandomWeighted(strategies, x => x.Value, totalWeight).Key;
    }

    protected virtual InterestPoint FindInterestPoint()
    {
        int tries = 16;

        for (int i = 0; i < tries; ++i)
        {
            InterestPoint p = SampleInterestPoint();

            // Make sure our interest point is not buried under the floor
            if (p)
            {
                Vector3 lowestPossiblePoint = p.transform.position - Vector3.up * p.size * p.transform.lossyScale.y * 2f;
                Vector3 skyPoint = lowestPossiblePoint;
                skyPoint.y = grid.bounds.size.y + grid.bounds.center.y;

                Ray ray = new Ray(skyPoint, -Vector3.up);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, Vector3.Distance(skyPoint, lowestPossiblePoint), 1 << LayerMask.NameToLayer("Floor")))
                {
                    //Debug.DrawLine(skyPoint, hit.point, Color.red, 60f);
                }
                else
                {
                    //Debug.DrawLine(skyPoint + Vector3.right, lowestPossiblePoint + Vector3.right, Color.cyan, 60f);
                    return p;
                }
            }
        }

        Debug.LogError("Could not find interest point!");

        return null;
    }

    /// <summary>
    /// This method has two main responsibilities:
    /// - Decide when to cut
    /// - Decide what shot to take
    /// It is tied to a specific event, so that the chaining of shots is possible
    /// </summary>
    protected ShotInformation TryFindCut(EmotionEvent e)
    {
        ShotInformation shot = new ShotInformation();
        shot.valid = false;
        shot.selectedCamera = null;
        shot.type = TransitionType.Cut; // TODO: for now...
        shot.strategy = null;

        CutRange searchRange = EvaluateCutRangeForEvent(e);
        float minT = e.timestamp + searchRange.minCutTime;
        float maxT = e.timestamp + searchRange.maxCutTime;

        List<EmotionEventGroup> searchEvents = ProceduralEngine.Instance.EventDispatcher.GetFutureEventGroups(minT, maxT);

        if (searchEvents.Count == 0)
            return shot;

        searchEvents = searchEvents.OrderBy(x => x.GetPriority()).ToList();

        // TODO: Add some bias based on proximity to current time?
        EmotionEventGroup selectedGroup = ProceduralEngine.SelectRandomWeighted(searchEvents, x => x.GetPriority());

        // We found a subset of interesting events, now we can pick something in here
        if(selectedGroup != null && selectedGroup.events.Count > 0)
        {
            EmotionEvent selectedEvent = ProceduralEngine.SelectRandomWeighted(selectedGroup.events, x => GetEventPriority(x));
            shot.duration = selectedEvent.timestamp - e.timestamp;

            // Try cutting before, but not after
            float margin = emotionEngine.BeatDurationNormalized * .5f;
            float fuzzyDuration = shot.duration - ProceduralEngine.RandomRange(0f, margin);

            if (fuzzyDuration > searchRange.minCutTime && fuzzyDuration < searchRange.maxCutTime)
                shot.duration = fuzzyDuration;

            int cameraTries = 10;

            for (int i = 0; i < cameraTries; ++i)
                if(shot.strategy == null)
                    shot.strategy = TryFindStrategy(selectedEvent, shot.duration * ProceduralEngine.Instance.Duration);
            
            shot.valid = shot.strategy != null;
        }

        if (shot.valid)
            shot.selectedCamera = InstanceCamera();

        return shot;
    }

    /// <summary>
    /// This method doesn't say the specific cut, but it constraints
    /// the time for searching interesting events. It is mostly
    /// dependent on current emotion.
    /// </summary>
    public CutRange EvaluateCutRangeForEvent(EmotionEvent e)
    {
        CutRange range = new CutRange();
        EmotionSpectrum emotionAtEventTime = emotionEngine.GetSpectrum(e.timestamp);
        CoreEmotion coreEmotion = EmotionEngine.FindMainEmotion(emotionAtEventTime);

        // In seconds
        switch (coreEmotion)
        {
            case CoreEmotion.Joy:
                range.minCutTime = ProceduralEngine.RandomRange(1f, 2f);
                range.maxCutTime = ProceduralEngine.RandomRange(7f, 8f);
                break;
            case CoreEmotion.Trust:
                range.minCutTime = ProceduralEngine.RandomRange(2f, 5f);
                range.maxCutTime = ProceduralEngine.RandomRange(7f, 10f);
                break;
            case CoreEmotion.Fear:
                range.minCutTime = ProceduralEngine.RandomRange(1f, 2f);
                range.maxCutTime = ProceduralEngine.RandomRange(4f, 6f);
                break;
            case CoreEmotion.Surprise:
                range.minCutTime = ProceduralEngine.RandomRange(.25f, .5f);
                range.maxCutTime = ProceduralEngine.RandomRange(1f, 2f);
                break;
            case CoreEmotion.Sadness:
                range.minCutTime = ProceduralEngine.RandomRange(1f, 1.5f);
                range.maxCutTime = ProceduralEngine.RandomRange(2f, 4f);
                break;
            case CoreEmotion.Disgust:
                range.minCutTime = ProceduralEngine.RandomRange(1f, 2f);
                range.maxCutTime = ProceduralEngine.RandomRange(3f, 4f);
                break;
            case CoreEmotion.Anger:
                range.minCutTime = ProceduralEngine.RandomRange(.1f, 1f);
                range.maxCutTime = ProceduralEngine.RandomRange(1f, 3f);
                break;
            case CoreEmotion.Anticipation:
                range.minCutTime = ProceduralEngine.RandomRange(.2f, .4f);
                range.maxCutTime = ProceduralEngine.RandomRange(1f, 3f);
                break;
        }

        switch (e.type)
        {
            case EmotionEvent.EmotionEventType.Start:
                // Longer cuts when showing for first time
                range.minCutTime *= e.chunkDelimitsSegment ? 1f : .75f;
                range.maxCutTime *= e.chunkDelimitsSegment ? 1f : .75f;
                break;
            case EmotionEvent.EmotionEventType.End:
                // Longer cuts when something disappears for good
                range.minCutTime *= e.chunkDelimitsSegment ? 1.5f : 1f;
                range.maxCutTime *= e.chunkDelimitsSegment ? 1.5f : 1f;
                break;
            case EmotionEvent.EmotionEventType.LocalMaximum:
                range.minCutTime *= 1f;
                range.maxCutTime *= 1f;
                break;
            case EmotionEvent.EmotionEventType.LocalMinimum:
                range.minCutTime *= 2f;
                range.maxCutTime *= 2f;
                break;
        }

        range.minCutTime = Mathf.Max(0.01f, range.minCutTime);
        range.maxCutTime = Mathf.Max(0.02f, range.maxCutTime);

        float tmp = range.minCutTime;
        range.minCutTime = Mathf.Min(range.minCutTime, range.maxCutTime);
        range.maxCutTime = Mathf.Max(tmp, range.maxCutTime);

        // Normalize times
        range.minCutTime /= ProceduralEngine.Instance.Duration;
        range.maxCutTime /= ProceduralEngine.Instance.Duration;
        return range;
    }

    protected void UpdateTransform()
    {
        //float transitionTime = 0f;

        //switch (currentCut.type)
        //{
        //    case TransitionType.Cut:
        //        transitionTime = 0f;
        //        break;
        //    case TransitionType.Blend:
        //        transitionTime = .5f;  // TODO: Procedural smooth time
        //        break;
        //}

        this.transform.position = currentShot.selectedCamera.transform.position;
        this.transform.rotation = currentShot.selectedCamera.transform.rotation;

        //if (currentCamera)
        //{
        //    smoothPosition.Target = currentCamera.EvaluateTargetPosition();
        //    smoothRotation.Target = currentCamera.EvaluateTargetRotation();
        //}

        //smoothPosition.Update(smoothTime, Time.deltaTime);
        //smoothRotation.Update(smoothTime, Time.deltaTime);

        //this.transform.position = smoothPosition.Value;
        //this.transform.rotation = smoothRotation.Value;
    }

    private void UpdateInterestPoints()
    {
        this.interestPoints = interestPoints.OrderByDescending(x => x.EvaluateHeuristic(true)).ToList();
    }

    public void UpdateCamera(float t)
    {
        if (!currentShot.valid)
            return;

        UpdateInterestPoints();
        UpdateTransform();

        if (currentCutTime < currentShot.duration)
        {
            // Normalized delta
            currentCutTime += Time.deltaTime / ProceduralEngine.Instance.Duration;

            if (!nextShot.valid)
                nextShot = TryFindCut(currentShot.selectedNextEventTrigger);
        }
        else
        {
            if(nextShot.valid)
                StartTransition(nextShot);
            else
                nextShot = TryFindCut(currentShot.selectedNextEventTrigger);
        }
    }

    public void OnEventDispatch(EmotionEvent e)
    {
        if (!currentShot.valid)
            StartTransition(TryFindCut(e));
    }

    public void OnEventGroupDispatch(EmotionEventGroup g)
    {
    }
}
