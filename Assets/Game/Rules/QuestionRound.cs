using System;
using System.Collections.Generic;
using MathQuestion;

namespace Game
{
    // 一局的出題順序與答題結果，不碰畫面
    public class QuestionRound
    {
        private readonly IReadOnlyList<Question> _questions;
        private int _index;
        private bool _missedCurrent;

        public QuestionRound(IReadOnlyList<Question> questions)
        {
            _questions = questions;
        }

        public int Count => _questions.Count;
        public bool IsFinished => _index >= _questions.Count;
        public Question Current => IsFinished ? null : _questions[_index];

        // 從 1 起算，給畫面顯示「第 n 題」用
        public int CurrentNumber => Math.Min(_index + 1, Count);

        // 重選後答對不算，企劃要看的是「第一次就答對」
        public int FirstTryCorrectCount { get; private set; }

        // 每次答對都餵得到，所以等於這局增加的飽足度
        public int FedCount { get; private set; }

        // 答對就進入下一題；答錯留在同一題
        public bool Submit(int choice)
        {
            if (IsFinished)
                throw new InvalidOperationException("這一局已經結束");

            if (!Current.IsCorrect(choice))
            {
                _missedCurrent = true;
                return false;
            }

            if (!_missedCurrent)
                FirstTryCorrectCount++;
            FedCount++;
            _index++;
            _missedCurrent = false;
            return true;
        }
    }
}
