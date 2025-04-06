[System.Serializable]
public enum DialogueCheck { NONE, MONEY, INFLUENCE }

[System.Serializable]
public class Dialogue
{
    public string dialogue;
    public Answer answer1;
    public Answer answer2;
    public Answer answer3;
    public Answer answer4;

    public Dialogue ()
    {

    }

    public Dialogue (string _dialogue, Answer _answer1, Answer _answer2, Answer _answer3, Answer _answer4)
    {
        dialogue = _dialogue;
        answer1 = _answer1;
        answer2 = _answer2;
        answer3 = _answer3;
        answer4 = _answer4;
    }
}

[System.Serializable]
public class Answer
{
    public string answerText;
    public AnswerActions action;
    public DialogueCheck check;
    public string answerFolder;
    public string negativeAnswerFolder;
    public string answerDialogue;
    public string negativeAnswerDialogue;
}
