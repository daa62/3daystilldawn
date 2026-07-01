using UnityEngine;

// The player's stranded friend. Interactable ([E]) — a short branching conversation
// that builds a "bond" and sets story flags in GameState, which later decide the
// ending. Put this on the NPC (same object as Npc) and set its layer to Interactable.
public class FriendNpc : MonoBehaviour, IInteractable
{
    [SerializeField] string friendName = "Mia";

    public string getPrompt() => "Talk to " + friendName;

    public void interact(PlayerInteractor interactor)
    {
        var dialogue = DialogueUI.Instance;
        if (dialogue == null || dialogue.IsOpen) return;

        var state = GameState.Instance;
        if (state != null && state.getFlag(GameManager.FLAG_FRIEND_MET))
            talkAgain(dialogue, state);
        else
            firstMeeting(dialogue, state);
    }

    // Opening conversation: one branching choice that sets the tone of the relationship.
    void firstMeeting(DialogueUI dialogue, GameState state)
    {
        dialogue.show(friendName, "You're alive... I really thought that horde got you.", () =>
            dialogue.showChoice(friendName,
                "My leg's wrecked — I can't run like this. What do we do?",
                new[]
                {
                    "We get out together. I'm not leaving you.",
                    "Stay and rest here. I'll find us a way out."
                },
                pick =>
                {
                    string reply;
                    if (pick == 0)
                    {
                        if (state != null)
                        {
                            state.addCounter(GameManager.COUNTER_BOND, 2);
                            state.setFlag(GameManager.FLAG_REASSURED);
                        }
                        reply = "...Okay. Together. Don't you dare leave me behind.";
                    }
                    else
                    {
                        if (state != null)
                        {
                            state.addCounter(GameManager.COUNTER_BOND, 1);
                            state.setFlag(GameManager.FLAG_FRIEND_RESTING);
                        }
                        reply = "Yeah... hurry. I don't feel so good.";
                    }

                    if (state != null) state.setFlag(GameManager.FLAG_FRIEND_MET);
                    dialogue.show(friendName, reply, () => dialogue.close());
                }));
    }

    // Later visits: reacts to how many supplies the player has gathered, and drops an
    // observational hint that the friend's condition is worsening (infection).
    void talkAgain(DialogueUI dialogue, GameState state)
    {
        int supplies = state != null ? state.getCounter(GameManager.COUNTER_SUPPLIES) : 0;
        string line = supplies >= GameManager.SUPPLIES_GOAL
            ? "You actually found enough to get us moving... maybe we'll make it. My fever's worse though — don't say it's nothing."
            : "Any luck out there? I don't think I can hold on much longer without meds.";
        dialogue.show(friendName, line, () => dialogue.close());
    }
}
