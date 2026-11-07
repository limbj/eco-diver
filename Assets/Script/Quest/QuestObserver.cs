using System.Collections;
using System.Collections.Generic;
using Script.Display;
using Script.Scene;
using UnityEngine;

namespace Script.Quest
{
    public class QuestObserver : MonoBehaviour
    {
        public static QuestObserver Instance
        {
            get
            {
                if (!_instance)
                {
                    _instance = FindObjectOfType<QuestObserver>();
                }

                return _instance;
            }
        }

        private static QuestObserver _instance;
        
        [SerializeField]
        private QuestArrowIndicator arrowIndicator;

        [SerializeField]
        private List<Quest> quests;
        
        private readonly Queue<Quest> _quests = new Queue<Quest>();
        private QuestLevel _questLevel;
        private Quest _currentQuest;
        private bool _isSuspended;
        private bool _hasFinishedQuests;

        private void Start()
        {
            if (quests != null)
            {
                foreach (var quest in quests)
                {
                    if (quest != null) RegisterQuest(quest);
                }
            }

            _questLevel = new QuestLevel(_quests.Count);
            StartQuest();
            UpdateDisplay();
        }

        private void RegisterQuest(Quest quest)
        {
            quest.Deactivate();
            _quests.Enqueue(quest);
        }

        public void SuspendForFailure()
        {
            _isSuspended = true;
            StopAllCoroutines();
            if (_currentQuest) _currentQuest.enabled = false;
            foreach (var quest in _quests)
                if (quest) quest.enabled = false;
            if (arrowIndicator) arrowIndicator.gameObject.SetActive(false);
        }

        public void UpdateQuest(Quest quest)
        {
            if (_isSuspended || _hasFinishedQuests || _quests.Count == 0 || quest != _currentQuest)
                return;

            _quests.Dequeue().Deactivate();
            _questLevel.LevelUp();

            OnCompleteQuest();
            StartQuest();
            UpdateDisplay();
        }

        private void StartQuest()
        {
            if (_isSuspended || _hasFinishedQuests) return;
            if (_quests.Count == 0)
            {
                OnFinishAllQuests();
                return;
            }

            _currentQuest = _quests.Peek();

            Debug.Log("Starting Quest: " + _currentQuest);
            _currentQuest.Activate();

            UpdateBackground(_currentQuest);
            UpdateMinimap(_currentQuest);
            UpdateStatusBar(_currentQuest);
            UpdateArrow(_currentQuest);
        }

        private void OnFinishAllQuests()
        {
            if (_isSuspended || _hasFinishedQuests || _currentQuest == null)
            {
                return;
            }
            _hasFinishedQuests = true;
            Debug.Log("All Quests Complete");
            if (arrowIndicator) arrowIndicator.gameObject.SetActive(false);
            StartCoroutine(WaitAndLoadScene(3));
        }

        private IEnumerator WaitAndLoadScene(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            if (!_isSuspended) SceneLoader.Instance.LoadNextScene();
        }

        private void UpdateArrow(Quest nextQuest)
        {
            if (arrowIndicator == null)
            {
                Debug.LogWarning("Arrow Indicator is not set.");
                return;
            }
            arrowIndicator.questTransform = nextQuest.GetTransform();
        }

        private void UpdateStatusBar(Quest nextQuest)
        {
            // todo: method stub
        }

        private void UpdateMinimap(Quest nextQuest)
        {
            // todo: implement if required
        }

        private void UpdateBackground(Quest nextQuest)
        {
            // todo: implement if required
        }

        private void OnCompleteQuest()
        {
            // todo: implement if required
        }

        private void UpdateDisplay()
        {
            var display = DisplayManager.Instance;
            if (!display) return;
            display.QuestLevel = _questLevel;
            display.QuestName = _currentQuest?.GetQuestName();
            display.QuestDescription = _currentQuest?.GetQuestDescription();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}
