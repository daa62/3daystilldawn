using System;
using System.Collections.Generic;
using UnityEngine;

// The player's stranded friend. Mornings play a scripted scene, nights open with
// a return line and then the action menu (talk / give items / rest).
// Lines wrapped in (parentheses) are stage directions and show without a speaker.
public class FriendNpc : MonoBehaviour, IInteractable
{
    const float LOOK_TURN_SPEED = 5f;   // how fast he turns to face you while talking

    [SerializeField] string friendName = "Samuel";

    bool talkedTonight;   // talk bonus applies once per night

    Transform playerBody;   // who to face while conversing
    bool conversing;        // only Samuel's own dialogue turns him — not notes/doors

    public string getPrompt() => "Talk to " + friendName;

    void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player != null) playerBody = player.transform;
    }

    // while his conversation is open, turn to face the player (FriendWander already
    // holds him still during dialogue, so nothing fights the rotation)
    void Update()
    {
        if (!conversing) return;

        var dialogue = DialogueUI.Instance;
        if (dialogue == null || !dialogue.IsOpen) { conversing = false; return; }
        if (playerBody == null) return;

        Vector3 to = playerBody.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(to), Time.deltaTime * LOOK_TURN_SPEED);
    }

    public void interact(PlayerInteractor interactor)
    {
        var dialogue = DialogueUI.Instance;
        if (dialogue == null || dialogue.IsOpen) return;

        conversing = true;
        var state = GameState.Instance;
        if (DayCycle.CurrentPhase == DayCycle.Phase.Night)
            nightCheckIn(dialogue, state, interactor.getInventory());
        else
            morning(dialogue, state);
    }

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

        // the intro is marked seen the moment it starts, so abandoning it partway
        // can't replay it on a later morning
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
                    "(Samuel has already wrapped his leg with cleaner bandages. He still looks tired, but he's sitting upright.)",
                    "Morning.",
                    "My leg's still awful... but I think the swelling's gone down a bit.",
                    "(He forces a smile.)",
                    "Maybe I'll be able to join you tomorrow." }
                : new[] {
                    "(Samuel is sitting where he slept. His breathing is shallow and his bandages are stained. He doesn't notice you right away.)",
                    "...Morning.",
                    "(He tries standing, but immediately sits back down.)",
                    "...I'm sorry, I'm still useless today." };
        }

        // day 3
        return cared
            ? new[] {
                "(Samuel looks exhausted. He keeps rubbing his arms despite the room being warm.)",
                "Weird…",
                "Huh? Oh, it's nothing. I'm just tired today.",
                "(He laughs quietly.)",
                "At least I can still complain." }
            : new[] {
                "(Samuel barely lifts his head. He takes several seconds before answering.)",
                "...You're leaving already?",
                "(He seems confused then realizes the time.)",
                "Oh.",
                "(He closes his eyes again.)" };
    }

    string[] morningNoMore(int day)
    {
        switch (day) {
            case 1: return new[] { "Thank you. I'm relying on you. It's okay if you come back early though… I'd like that." };
            case 2: return new[] { "Be careful out there." };
            default: return new[] { "…", "(Samuel seems to be resting again, better not disturb him.)" };
        }
    }

    // day 1 opening: two choice points that set the tone of the relationship
    void day1Intro(DialogueUI dialogue, GameState state)
    {
        dialogue.show("", "(Samuel is sitting against the wall, one hand pressed against his injured leg. He tries to smile when he notices you.)", () =>
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

    // talkedTonight is not reset here — reopening the conversation must not farm the
    // talk bonus. It resets anyway each night since returning reloads the scene.
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
            labels.Add("Give him something");
            actions.Add(() => giveMenu(dialogue, state, inventory));
        }

        labels.Add("Rest until morning.");
        actions.Add(() => confirmRest(dialogue, state, inventory));

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

    // every carried givable item gets its own row, duplicates included
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
                        GameManager.FRIEND_HEALTH_FOOD, 0, giveResponse(captured, DayCycle.CurrentDay)));
                    break;
                case ItemType.Medicine:
                    actions.Add(() => giveItem(dialogue, state, inventory, captured,
                        GameManager.FRIEND_HEALTH_MEDICINE, 0, giveResponse(captured, DayCycle.CurrentDay)));
                    break;
                default:   // Comfort
                    actions.Add(() => giveItem(dialogue, state, inventory, captured,
                        0, GameManager.BOND_COMFORT_ITEM, giveResponse(captured, DayCycle.CurrentDay)));
                    break;
            }
        }

        labels.Add("Never mind.");
        actions.Add(() => nightMenu(dialogue, state, inventory));

        dialogue.showChoice(friendName, "What do you want to give him?",
                            labels.ToArray(), pick => actions[pick]());
    }

    // Per-item, per-night reactions from the Project 2 dialogue script. Keyed on the
    // item's display name; anything without bespoke lines falls back to the food/meds
    // stage directions so new items still read sensibly.
    string[] giveResponse(ItemData item, int day)
    {
        switch (item.itemName)
        {
            case "Water Bottle":
            case "Soda":
            case "Juice":
            case "Milk":
                switch (day) {
                    case 1:  return new[] { "(He drinks carefully, then lets out a quiet breath.)" };
                    case 2:  return new[] { "(He swallows with some effort before lowering the bottle.)" };
                    default: return new[] { "(He takes one small sip then quietly shakes his head.)" };
                }
            case "Medkit":
                switch (day) {
                    case 1:  return new[] { "(He studies the items inside then quietly cleans and redresses his wound.)" };
                    case 2:  return new[] { "(His hands tremble as he works, but he quietly finishes cleaning and redressing his wound.)" };
                    default: return new[] { "(He accepts it, turning it over in his hands before carefully placing it within reach.)" };
                }
            case "Antibiotics":
            case "Bottle of Pills":
            case "Pills":
            case "Medicine":
                switch (day) {
                    case 1:  return new[] { "(He studies the label for a moment before swallowing one.)" };
                    case 2:  return new[] { "(He shakes out a pill without checking the label and swallows it with difficulty.)" };
                    default: return new[] { "(He accepts it, turning it over in his hands before carefully placing it within reach.)" };
                }
            case "Plush Bear":
                switch (day) {
                    case 1:  return new[] {
                        "(He looks at the plush bear then laughs softly while hugging it close.)",
                        "Finally. A real bear.",
                        "I should've brought this on that camping trip.",
                        "Would've saved us a lot of arguing." };
                    case 2:  return new[] {
                        "(He smiles faintly at the bear and pats its head.)",
                        "Oh, it's a proper bear and not a raccoon this time." };
                    default: return new[] {
                        "(He brushes the fur with a light hand.)",
                        "…A real bear." };
                }
            case "Deck of Cards":
                switch (day) {
                    case 1:  return new[] {
                        "Remember when we used to play during road trips?",
                        "You'd get so competitive but never win against me.",
                        "Watching you sulk while building a tower of cards was funny." };
                    case 2:  return new[] {
                        "You never did beat me.",
                        "I don't think you ever will." };
                    default: return new[] {
                        "(He traces the edge of a card with his thumb.)",
                        "…Still undefeated." };
                }
            case "Camping Lantern":
                switch (day) {
                    case 1:  return new[] {
                        "This reminds me of when you insisted we try the \"old fashioned\" way of camping.",
                        "You tried so hard to rub wooden sticks together for our fire.",
                        "Thankfully I brought an oil lantern." };
                    case 2:  return new[] {
                        "You really thought rubbing sticks together would work.",
                        "Good thing one of us came prepared with an oil lantern." };
                    default: return new[] {
                        "(He watches the lantern's light flicker.)",
                        "…I did a good job bringing one back then." };
                }
            case "Bandages":
                switch (day) {
                    case 1:  return new[] { "(He turns away from you and carefully wraps the fresh bandage around the wound.)" };
                    case 2:  return new[] { "(He turns away with effort and replaces the old bandage with trembling hands.)" };
                    default: return new[] { "(He accepts it, turning it over in his hands before carefully placing it within reach.)" };
                }
            case "Ointment":
                switch (day) {
                    case 1:  return new[] { "(He turns away from you to gently spread the ointment over the wound and winces.)" };
                    case 2:  return new[] { "(He turns away with effort and applies the ointment with trembling hands.)" };
                    default: return new[] { "(He accepts it, turning it over in his hands before carefully placing it within reach.)" };
                }
            case "Birthday Card":
                switch (day) {
                    case 1:  return new[] {
                        "\"Happy Birthday.\"",
                        "(He opens the card, looks at the back, then flips it around again.)",
                        "That's it? No tacky glitter, terrible jokes, or embarrassing pictures?",
                        "My cards were way better than this." };
                    case 2:  return new[] {
                        "\"Happy Birthday,\" huh…",
                        "I still think birthday cards should have terrible jokes.",
                        "They have more personality that way." };
                    default: return new[] {
                        "(He looks at the card for a long moment.)",
                        "…Mine were better." };
                }
            case "Handheld Radio":
                switch (day) {
                    case 1:  return new[] {
                        "(He presses the button, listening to the static.)",
                        "Remember when we used these as kids?",
                        "You'd hide somewhere and make me \"search and rescue\" you.",
                        "…I still think secretly running around and changing spots is unfair." };
                    case 2:  return new[] {
                        "(He presses the button, listening to the static.)",
                        "Remember \"search and rescue\"? It was fun.",
                        "…Still think changing spots was cheating though." };
                    default: return new[] {
                        "(He feels the bevel of the button without pressing it.)",
                        "…It was fun, even if you were a cheater." };
                }
            case "Pocket Tent":
                switch (day) {
                    case 1:  return new[] {
                        "This reminds me of our first camping trip.",
                        "We bought the cheapest tent we could find. Big mistake.",
                        "It leaked, the zipper broke, and I swear the wind almost took it away.",
                        "We pooled our money together to get a better one next time we went." };
                    case 2:  return new[] {
                        "Our first tent was awful.",
                        "We really learned not to buy the cheapest one." };
                    default: return new[] {
                        "(He runs his hand across the logo.)",
                        "…The second one was worth it." };
                }
            case "Polaroid Photo":
                switch (day) {
                    case 1:  return new[] {
                        "I used to be so obsessed with these.",
                        "I think I spent more on the film than the actual camera.",
                        "…You kept all the ones I took of you?",
                        "Thanks. I hope I can take more someday." };
                    case 2:  return new[] {
                        "I used to have so many of these.",
                        "…You kept the ones I took?",
                        "I'm glad." };
                    default: return new[] {
                        "(He smiles faintly at the familiar glossy texture.)",
                        "…I miss my collection." };
                }
            case "Raccoon Plush":
                switch (day) {
                    case 1:  return new[] {
                        "(He laughs quietly.)",
                        "Oh no. Don't tell me you still think these things are basically bears.",
                        "\"It's like a small bear, Sam.\"",
                        "It was absolutely not a bear." };
                    case 2:  return new[] {
                        "(He laughs quietly.)",
                        "You're never going to convince me.",
                        "This is a raccoon, not a bear." };
                    default: return new[] {
                        "(He chuckles quietly.)",
                        "…A plush \"basically a bear.\"" };
                }
            case "Wrapped Gift Box":
                switch (day) {
                    case 1:  return new[] {
                        "Remember how you always got excited before opening presents?",
                        "You'd always shake the box and try to guess what was inside.",
                        "You were never right though." };
                    case 2:  return new[] {
                        "You always thought shaking it would help.",
                        "It never did." };
                    default: return new[] {
                        "(He taps the lid twice.)",
                        "…Still guessing?" };
                }
            default:   // anything without bespoke lines falls back by type
                switch (item.type) {
                    case ItemType.Medicine:
                        return new[] { "(He accepts it, turning it over in his hands before carefully placing it within reach.)" };
                    case ItemType.Comfort:   // e.g. the novel — a keepsake, not a meal
                        switch (day) {
                            case 1:  return new[] { $"(He turns the {item.itemName.ToLower()} over in his hands, smiling at some memory it stirs.)" };
                            case 2:  return new[] { $"(He holds the {item.itemName.ToLower()} for a while, quiet but a little lighter.)" };
                            default: return new[] { $"(He keeps the {item.itemName.ToLower()} close, resting a hand on it.)", "…Thank you." };
                        }
                    default:   // Survival: food
                        switch (day) {
                            case 1:  return new[] { "(He hesitates before taking it, then eats with small bites.)" };
                            case 2:  return new[] { "(He eats slowly, like he has to remember how.)" };
                            default: return new[] { "(He forces himself to take a few bites before quietly setting the can aside.)" };
                        }
                }
        }
    }

    void giveItem(DialogueUI dialogue, GameState state, Inventory inventory, ItemData item,
                  int healthDelta, int bondDelta, string[] response)
    {
        inventory.removeItem(item);
        addClamped(state, GameManager.COUNTER_FRIEND_HEALTH, healthDelta);
        addClamped(state, GameManager.COUNTER_BOND, bondDelta);
        if (healthDelta > 0) state?.setFlag(GameManager.FLAG_CARED_OVERNIGHT);

        showSequence(dialogue, response, 0, () => nightMenu(dialogue, state, inventory));
    }

    void talkTonight(DialogueUI dialogue, GameState state, Inventory inventory)
    {
        if (talkedTonight) {
            // the long story only plays once a night
            dialogue.show("", "(He smiles faintly, but seems too tired to keep talking.)",
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
                "(He slowly pulls back the bandage around his leg, revealing a rotting bite wound.)",
                "I wasn't hurt when we were running.",
                "…I was bitten.",
                "(He takes a long, shaky breath.)",
                "I wanted to believe... if I didn't say it out loud... maybe it wasn't real.",
                "…I'm sorry." };
        }
    }

    // the third night's rest is the point of no return — whatever Samuel has been
    // given by now is what decides the ending, so make sure the player means it
    void confirmRest(DialogueUI dialogue, GameState state, Inventory inventory)
    {
        if (DayCycle.CurrentDay < GameManager.TOTAL_DAYS) {
            rest(dialogue, state);
            return;
        }

        dialogue.showChoice("",
            "This is the last night. Once you sleep, whatever you've done for Samuel is all that will count when rescue comes.",
            new[] { "Rest — see it through.", "Not yet." },
            pick =>
            {
                if (pick == 0) rest(dialogue, state);
                else nightMenu(dialogue, state, inventory);
            });
    }

    void rest(DialogueUI dialogue, GameState state)
    {
        // the early-return bond banks here, when the evening is actually spent together
        if (state != null) {
            int pending = state.getCounter(GameManager.COUNTER_LAST_RUN_BOND);
            if (pending > 0) addClamped(state, GameManager.COUNTER_BOND, pending);
        }

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
            if (DayCycle.CurrentDay >= GameManager.TOTAL_DAYS)
                DayCycle.resolveNight();                            // final night loads the Ending, which fades on its own
            else
                SceneTransition.fadeThrough(() =>
                {
                    // behind the black: the night resolves and the player wakes
                    // up at their sleeping spot instead of mid-room
                    DayCycle.resolveNight();
                    SpawnPoint.movePlayerTo(SpawnPoint.WAKE_UP);
                });
        });
    }

    static void addClamped(GameState state, string key, int delta)
    {
        if (state == null || delta == 0) return;
        int value = Mathf.Clamp(state.getCounter(key) + delta, 0, GameManager.FRIEND_STAT_MAX);
        state.setCounter(key, value);
    }
}
