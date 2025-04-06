using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class DialogueManager : SerializedSingleton<DialogueManager>
{
    public bool isDialogueOpen;
    public List<string> shownDialogues;
    public List<Dialogue> dialogues;
    public Dialogue dialogue;

    [SerializeField] private List<string> dialogueQueue = new List<string>();

    public VillagerScript currentSpeaker = null;
    public Business currentBusiness = null;
    public string currentDialogue;
    public string dialogueName, dialoguePath;

    bool isFilling;
    string fillerString;
    private char currentChar;
    private string startTag, endTag;

    public bool HasCurrentBusiness()
    {
        if (currentBusiness != null && currentBusiness.title != "")
            return true;
        return false;
    }


    private void Update()
    {
        if (isFilling)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyUp(KeyCode.Space))
            {
                currentDialogue = fillerString;
                isFilling = false;
                StopAllCoroutines();
            }
        }
    }

    public bool IsDialogueShown(string path)
    {
        return shownDialogues.Contains(path);
    }

    public void RemoveDialogue(string path)
    {
        if (IsDialogueShown(path))
        {
            shownDialogues.Remove(path);
        }
    }

    public void ClearQueue()
    {
        dialogueQueue.Clear();
    }

    public void DialogueOpener()
    {
        TextFiller(L.G(dialogue.dialogue));
    }

    private string LastShownDialogue()
    {
        if (shownDialogues.Count < 1)
            return "";
        return shownDialogues[shownDialogues.Count - 1];
    }

    private string PrelastShownDialogue()
    {
        if (shownDialogues.Count < 2)
            return "";
        return shownDialogues[shownDialogues.Count - 2];
    }

    public Dialogue GetDialogue()
    {
        if (dialogue == null)
            return null;
        return dialogue;
    }

    public void LoadDialogue (string path, Business speaker)
    {
        currentBusiness = speaker;
        LoadDialogue(path, null, true);
    }

    public void LoadDialogue(string path, VillagerScript speaker = null, bool shouldOpen = true, bool mandatory = false)
    {
        if (path == string.Empty)
            return;
        /*if (mandatory)
        {
            if (shownDialogues.Contains(path))
                shownDialogues.Remove(path);
        }*/
        //if (!IsDialogueShown(path))
        {
            if (speaker != null)
            {
                currentSpeaker = speaker;
            }
            int lastIndex = path.LastIndexOf('/');
            if (lastIndex != -1)
            {
                dialogueName = path.Substring(lastIndex + 1);
                dialoguePath = path.Substring(0, lastIndex);
            }
            isDialogueOpen = true;
            dialogues.Clear();
            dialogues.AddRange(TypeArrayDeserializer.LoadResourceFromJson<Dialogue>(path));
            foreach (var d in dialogues)
            {
                if (L.G(d.dialogue).Contains ("FROM"))
                {
                    d.dialogue = d.dialogue.Replace("FROM", string.Format("<color=yellow>{0}</color>", currentSpeaker.GetStartingTown()));
                }
                if (L.G(d.dialogue).Contains("DESTINATION"))
                {
                    d.dialogue = d.dialogue.Replace("DESTINATION", string.Format("<color=yellow>{0}</color>", currentSpeaker.GetDestinationTown()));
                }
                if (L.G(d.dialogue).Contains("PRODUCTS"))
                {
                    if (currentSpeaker.GetProducts() == "")
                    {
                        d.dialogue = d.dialogue.Replace("PRODUCTS", L.G("nothing of your interest now."));
                    }
                    else
                    {
                        d.dialogue = d.dialogue.Replace("PRODUCTS", currentSpeaker.GetProducts());
                    }
                }
                if (L.G(d.dialogue).Contains("HOUSE"))
                {
                    try
                    {
                        d.dialogue = d.dialogue.Replace("HOUSE", string.Format("<color=yellow>{0}</color>", L.G(currentSpeaker.GetFaction().name)));
                    }
                    catch
                    {
                        d.dialogue = d.dialogue.Replace("HOUSE", string.Format("<color=yellow>{0}</color>", L.G(currentBusiness.thisTown.thisTown.GetFaction().name)));
                    }
                }
                if (L.G(d.dialogue).Contains("MOTTO"))
                {
                    d.dialogue = d.dialogue.Replace("MOTTO", L.G(currentSpeaker.GetFaction().motto));
                }
                if (L.G(d.dialogue).Contains("BUSINESSTITLE"))
                {
                    d.dialogue = d.dialogue.Replace("BUSINESSTITLE", L.G(currentBusiness.title));
                }
                if (L.G(d.dialogue).Contains("BUSINESSPRICE"))
                {
                    d.dialogue = d.dialogue.Replace("BUSINESSPRICE", currentBusiness.GetBusinessPrice().ToString());
                }
            }
            dialogue = dialogues[UnityEngine.Random.Range(0, dialogues.Count)];
            InterfaceHandler.Instance.ActivateDialogue(dialogue);
            //shownDialogues.Add(path);
            if (dialogueQueue.Contains(path))
            {
                dialogueQueue.Remove(path);
            }
            if (shouldOpen)
                DialogueOpener();
            Debug.Log(string.Format("Loading dialogue: {0}", path));
        }
    }

    public bool IsSkippable()
    {
        return true;
    }

    void TextFiller(string text)
    {
        if (!isFilling)
        {
            currentDialogue = string.Empty;
            isFilling = true;
            fillerString = L.G(text);
            StartCoroutine(WriteText(fillerString));
        }
    }


    private IEnumerator WriteText(string text)
    {
        startTag = string.Empty;
        endTag = string.Empty;

        for (int i = 0; i < text.Length; i++)
        {
            currentChar = text[i];

            if (currentChar == '<')
            {
                if (string.IsNullOrEmpty(startTag))
                {
                    int currentIndex = i;

                    for (int j = currentIndex; j < text.Length; j++)
                    {
                        startTag += text[j].ToString();

                        if (text[j] == '>')
                        {
                            currentIndex = j;
                            i = currentIndex;

                            for (int k = currentIndex; k < text.Length; k++)
                            {
                                char next = text[k];

                                if (next == '<')
                                    break;

                                currentIndex++;
                            }
                            break;
                        }
                    }

                    for (int j = currentIndex; j < text.Length; j++)
                    {
                        endTag += text[j].ToString();

                        if (text[j] == '>')
                        {
                            break;
                        }
                    }
                }
                else
                {
                    for (int j = i; j < text.Length; j++)
                    {
                        if (text[j] == '>')
                        {
                            i = j;
                            break;
                        }
                    }
                    startTag = string.Empty;
                    endTag = string.Empty;
                }
                continue;
            }

            currentDialogue += string.Format("{0}{1}{2}", startTag, currentChar, endTag);

            yield return new WaitForSecondsRealtime(0.02f);
        }
        isFilling = false;
    }
}
