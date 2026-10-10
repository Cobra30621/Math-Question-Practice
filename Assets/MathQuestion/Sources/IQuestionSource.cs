using System.Collections.Generic;
using UnityEngine;

namespace MathQuestion
{
    public interface IQuestionSource
    {
        Awaitable<IReadOnlyList<Question>> GetQuestions(QuestionFilter filter, int count);
    }
}
