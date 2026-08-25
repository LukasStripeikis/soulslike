using StateId = System.UInt32;
public class State
{
    private StateId id;
    private float stateDuration;
    public const float INDEFINITE_STATE_DURATION = -1;
    public State(StateId id, float stateDuration)
    {
        this.id = id;
        this.stateDuration = stateDuration;
    }

    public StateId GetId() { return id; }
    public float GetStateDuration() { return stateDuration; }
    public bool IsLimitedTimeState() { return stateDuration >= 0.0f; }
}

public class StateTransition
{
    private StateId startStateId;
    private StateId endStateId;
    private float time;

    public StateTransition(StateId startStateId, StateId endStateId, float time)
    {
        this.startStateId = startStateId;
        this.endStateId = endStateId;
        this.time = time;
    }

    public StateId GetStartStateId() { return startStateId; }
    public StateId GetEndStateId() { return endStateId; }
    public float GetTransitionTime() { return time; }
}

public class StateMachine
{
    private State[] states;
    private State currentState;
    private State nextState;
    private StateTransition[] transitions;
    private StateTransition currentTransition;
    private float time;

    public StateMachine(State[] states, StateTransition[] transitions, StateId startStateId)
    {
        this.states = states;
        currentState = FindState(startStateId);
        nextState = null;

        this.transitions = transitions;
        currentTransition = null;
        time = 0.0f;
    }

    public void Update(float deltaTime)
    {
        if (currentState.IsLimitedTimeState() && nextState != null)
        {
            time += deltaTime;
            if (time >= currentState.GetStateDuration())
            {
                time = 0.0f;
                SetState(nextState);
            }
        }
        else if (IsInTransition())
        {
            time += deltaTime;
            if (time >= currentTransition.GetTransitionTime())
            {
                time = 0.0f;
                SetState(nextState);
            }
        }
    }

    public State GetCurrentState() { return currentState; }
    public bool IsState(StateId stateId)
    {
        return currentState.GetId() == stateId;
    }
    public State FindState(StateId stateId)
    {
        //TODO: use bianry search or table lookup instead
        foreach (State state in states)
        {
            if (state.GetId() == stateId)
                return state;
        }
        return null;
    }

    public bool IsInTransition() { return currentTransition != null && nextState != null; }
    public StateTransition FindTransition(StateId startStateId, StateId endStateId)
    {
        foreach (StateTransition transition in transitions)
        {
            if (transition.GetStartStateId() == startStateId &&
                transition.GetEndStateId() == endStateId)
                return transition;
        }
        return null;
    }

    public bool TrySetState(StateId newStateId)
    {
        State newState = FindState(newStateId);
        if (newState == null || newState == currentState)
            return false;

        StateTransition transition = FindTransition(currentState.GetId(), newStateId);
        if (transition != null)
        {
            currentTransition = transition;
            nextState = newState;
        }
        else SetState(newState);

        return true;
    }
    private void SetState(State state)
    {
        currentState = state;
        nextState = null;
    }
}