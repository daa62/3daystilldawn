using System;
using System.Collections.Generic;
using UnityEngine;

// The player's stranded friend. Interactable ([E]). Narrative dialogue comes first:
// each morning has a scripted scene (day 2/3 varying on whether she was cared for the
// night before), each night opens with a scripted early/late-return line — and only
// after the narrative beat does the night action menu (talk / give items / rest)
// appear. Lines in (parentheses) are stage directions and show without a speaker.
// Put this on the NPC (same object as Npc), layer Interactable.
public class FriendNpc : MonoBehaviour, IInteractable
{
    [SerializeField] string friendName = "Mia";

    bool talkedTonight;   // the +bond (and the long story) for talking applies once per night

    public string getPrompt() => "Talk to " + friendName;

    public void interact(PlayerInteractor interactor)
    {
        var dialogue = DialogueUI.Instance;
        if (dialogue == null || dialogue.IsOpen) return;

        var state = GameState.Instance;
        if (DayCycle.CurrentPhase == DayCycle.Phase.Night)
            nightCheckIn(dialogue, state, interactor.getInventory());
        else
            morning(dialogue, state);
    }

    // Plays lines in order; ()-wrapped lines are unspoken stage directions.
    void showSequence(DialogueUI dialogue, string[] lines, int index, Action done)
    {
        if (index >= lines.Length) { done(); return; }
        string speaker = lines[index].StartsWith("(") ? "" : friendName;
        dialogue.show(speaker, lines[index], () => showSequence(dialogue, lines, index + 1, done));
    }

    // ---------------------------------------------------------------- mornings

    void morning(DialogueUI dialogue, GameState state)
    {
        int day = DayCycle.CurrentDay;
        string talkedFlag = GameManager.MORNING_TALKED_PREFIX + day;

        // The opening scene belongs to day 1 only, and is marked as seen the moment it
        // starts — abandoning it partway (or skipping day 1's chat entirely) must not
        // replay it on a later morning.
        if (state != null && day == 1 && !state.getFlag(GameManager.FLAG_FRIEND_MET)) {
            state.setFlag(GameManager.FLAG_FRIEND_MET);
            state.setFlag(talkedFlag);   // re-talking today lands on the no-more line
            day1Intro(dialogue, state);
            return;
        }

        if (state != null && !state.getFlag(talkedFlag)) {
            state.setFlag(talkedFlag);
            bool cared = state.getFlag(GameManager.FLAG_CARED_OVERNIGHT);
            showSequence(dialogue, morningScene(day, cared), 0, dialogue.close);
            return;
        }

        // no more dialogue today
        showSequence(dialogue, morningNoMore(day), 0, dialogue.close);
    }

    string[] morningScene(int day, bool cared)
    {
        if (day == 2) {
            return cared
                ? new[] {
                    "(Mia has already wrapped her leg with cleaner bandages. She still looks tired, but she's sitting upright.)",
                    "Morning.",
                    "My leg's still awful... but I think the swelling's gone down a bit.",
                    "(She forces a smile.)",
                    "Maybe I'll be able to join you tomorrow." }
                : new[] {
                    "(Mia is sitting where she slept. Her breathing is shallow and her bandages are stained. She doesn't notice you right away.)",
                    "...Morning.",
                    "(She tries standing, but immediately sits back down.)",
                    "...I'm sorry, I'm still useless today." };
        }

        // day 3
        return cared
            ? new[] {
                "(Mia looks exhausted. She keeps rubbing her arms despite the room being warm.)",
                "Weird…",
                "Huh? Oh, it's nothing. I'm just tired today.",
                "(She laughs quietly.)",
                "At least I can still complain." }
            : new[] {
                "(Mia barely lifts her head. She takes several seconds before answering.)",
                "...You're leaving already?",
                "(She seems confused then realizes the time.)",
                "Oh.",
                "(She closes her eyes again.)" };
    }

    string[] morningNoMore(int day)
    {
        switch (day) {
            case 1: return new[] { "Thank you. I'm relying on you. It's okay if you come back early though… I'd like that." };
            case 2: return new[] { "Be careful out there." };
            default: return new[] { "…", "(Mia seems to be resting again, better not disturb her.)" };
        }
    }

    // Day 1 opening scene: two choice points that set the tone of the relationship.
    void day1Intro(DialogueUI dialogue, GameState state)
    {
        dialogue.show("", "(Mia is sitting against the wall, one hand pressed against her injured leg. She tries to smile when she notices you.)", () =>
            dialogue.showChoice(friendName,
                "Do you really think help will come for us?",
                new[]
                {
                    "We can only hope for the best.",
                    "I believe in the soldier, help will come."
                },
                pick =>
                {
                    string reply;
                    if (pick == 0)
                        reply = "Yeah… I'll try to stay positive.";
                    else {
                        addClamped(state, GameManager.COUNTER_BOND, 1);
                        reply = "It feels reassuring when you say it.";
                    }

                    dialogue.show(friendName, reply, () =>
                        dialogue.showChoice(friendName,
                            "But, my leg's wrecked–I can't run like this. What do we do?",
                            new[]
                            {
                                "I'll search the mall, you stay here where it's safe.",
                                "Leave everything to me. We'll make it out together."
                            },
                            pick2 =>
                            {
                                string reply2;
                                if (pick2 == 0) {
                                    addClamped(state, GameManager.COUNTER_BOND, 1);
                                    state?.setFlag(GameManager.FLAG_FRIEND_RESTING);
                                    reply2 = "…Okay. Just don't do anything reckless.";
                                }
                                else {
                                    addClamped(state, GameManager.COUNTER_BOND, 2);
                                    state?.setFlag(GameManager.FLAG_REASSURED);
                                    reply2 = "…Okay. Together. I trust you.";
                                }

                                dialogue.show(friendName, reply2, () => dialogue.close());
                            }));
                }));
    }

    // ---------------------------------------------------------------- nights

    // talkedTonight is NOT reset here: the player can now leave and restart this
    // conversation, and the once-a-night talk bonus must not farm. The field resets
    // naturally each night because returning from the store reloads the scene.
    void nightCheckIn(DialogueUI dialogue, GameState state, Inventory inventory)
    {
        bool early = state != null && state.getCounter(GameManager.COUNTER_LAST_RUN_BOND) > 0;
        dialogue.show(friendName, nightOpener(DayCycle.CurrentDay, early),
                      () => nightMenu(dialogue, state, inventory));
    }

    string nightOpener(int day, bool early)
    {
        switch (day) {
            case 1: return early
                ? "You're early! Is it because of what I said?"
                : "Oh good, you're back. I was getting worried.";
            case 2: return early
                ? "You're back early… Did something happen out there?"
                : "I was afraid… No, it's nothing. I'm glad you're back.";
            default: return early
                ? "…You came back. That's good."
                : "…I thought I would never see you again.";
        }
    }

    void nightMenu(DialogueUI dialogue, GameState state, Inventory inventory)
    {
        var labels  = new List<string>();
        var actions = new List<Action>();

        labels.Add("Talk");
        actions.Add(() => talkTonight(dialogue, state, inventory));

        if (hasGivableItem(inventory)) {
            labels.Add("Give her something");
            actions.Add(() => giveMenu(dialogue, state, inventory));
        }

        labels.Add("Rest until morning.");
        actions.Add(() => rest(dialogue, state));

        labels.Add("Leave the conversation.");
        actions.Add(() => dialogue.close());

        dialogue.showChoice(friendName, "Did you find anything useful today?",
                            labels.ToArray(), pick => actions[pick]());
    }

    static bool isGivable(ItemData item) =>
        item.type == ItemType.Survival || item.type == ItemType.Medicine || item.type == ItemType.Comfort;

    bool hasGivableItem(Inventory inventory)
    {
        if (inventory == null) return false;
        foreach (ItemData item in inventory.getItems())
            if (isGivable(item)) return true;
        return false;
    }

    // Second page: what to hand over. Every carried givable item gets its own row
    // (duplicates included — two food cans are two rows), plus a way back.
    void giveMenu(DialogueUI dialogue, GameState state, Inventory inventory)
    {
        var labels  = new List<string>();
        var actions = new List<Action>();

        foreach (ItemData item in inventory.getItems())
        {
            if (!isGivable(item)) continue;

            ItemData captured = item;   // each closure needs its own item
            labels.Add($"The {item.itemName}");

            switch (item.type)
            {
                case ItemType.Survival:
                    actions.Add(() => giveItem(dialogue, state, inventory, captured,
                        GameManager.FRIEND_HEALTH_FOOD, 0, foodResponse(DayCycle.CurrentDay)));
                    break;
                case ItemType.Medicine:
                    actions.Add(() => giveItem(dialogue, state, inventory, captured,
                        GameManager.FRIEND_HEALTH_MEDICINE, 0,
                        "(She swallows the meds without a word. Her breathing evens out, just slightly.)"));
                    break;
                default:   // Comfort
                    actions.Add(() => giveItem(dialogue, state, inventory, captured,
                        0, GameManager.BOND_COMFORT_ITEM,
                        $"(She stares at the {captured.itemName} for a long moment. \"You remembered,\" she says, and something in her settles.)"));
                    break;
            }
        }

        labels.Add("Never mind.");
        actions.Add(() => nightMenu(dialogue, state, inventory));

        dialogue.showChoice(friendName, "What do you want to give her?",
                            labels.ToArray(), pick => actions[pick]());
    }

    string foodResponse(int day)
    {
        switch (day) {
            case 1: return "(She hesitates before taking it, then eats with small bites.)";
            case 2: return "(She eats slowly, like she has to remember how.)";
            default: return "(She forces herself to take a few bites before quietly setting the can aside.)";
        }
    }

    void giveItem(DialogueUI dialogue, GameState state, Inventory inventory, ItemData item,
                  int healthDelta, int bondDelta, string response)
    {
        inventory.removeItem(item);
        addClamped(state, GameManager.COUNTER_FRIEND_HEALTH, healthDelta);
        addClamped(state, GameManager.COUNTER_BOND, bondDelta);
        if (healthDelta > 0) state?.setFlag(GameManager.FLAG_CARED_OVERNIGHT);

        dialogue.show("", response, () => nightMenu(dialogue, state, inventory));
    }

    void talkTonight(DialogueUI dialogue, GameState state, Inventory inventory)
    {
        if (talkedTonight) {
            // the long story only plays once a night
            dialogue.show("", "(She smiles faintly, but seems too tired to keep talking.)",
                          () => nightMenu(dialogue, state, inventory));
            return;
        }

        talkedTonight = true;
        addClamped(state, GameManager.COUNTER_BOND, GameManager.BOND_TALK_AT_NIGHT);
        showSequence(dialogue, nightTalk(DayCycle.CurrentDay), 0,
                     () => nightMenu(dialogue, state, inventory));
    }

    string[] nightTalk(int day)
    {
        switch (day) {
            case 1: return new[] {
                "I had a lot of time to reminisce about the past today.",
                "Remember the road trip where we lost the tent?",
                "It was a disaster. You swore even the raccoon was “basically a bear”!",
                "…We're getting out of this one too." };
            case 2: return new[] {
                "I remember coming to this mall to shop for my mom's birthday present once.",
                "It was so crowded and lively…",
                "It's still crowded now–just a lot more dead." };
            default: return new[] {
                "...There's something I should've told you.",
                "(She slowly pulls back the bandage around her leg, revealing a rotting bite wound.)",
                "I wasn't hurt when we were running.",
                "…I was bitten.",
                "(She takes a long, shaky breath.)",
                "I wanted to believe... if I didn't say it out loud... maybe it wasn't real.",
                "…I'm sorry." };
        }
    }

    void rest(DialogueUI dialogue, GameState state)
    {
        string[] lines;
        switch (DayCycle.CurrentDay) {
            case 1:
                lines = new[] { "Get some sleep. I'll… try to do the same." };
                break;
            case 2:
                lines = new[] { "You should sleep.", "…I'll stay awake a little longer." };
                break;
            default:
                lines = new[] {
                    "You should get some sleep…",
                    "Regardless of if we get saved tomorrow… I'll always be grateful to you.",
                    "Thank you." };
                break;
        }

        showSequence(dialogue, lines, 0, () =>
        {
            dialogue.close();
            DayCycle.resolveNight();
        });
    }

    static void addClamped(GameState state, string key, int delta)
    {
        if (state == null || delta == 0) return;
        int value = Mathf.Clamp(state.getCounter(key) + delta, 0, GameManager.FRIEND_STAT_MAX);
        state.setCounter(key, value);
    }
}
