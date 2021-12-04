
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using FSM;

namespace FSM.Tests
{
    public class Recorder<TStateId> {
        private enum RecordedAction {
            ENTER, LOGIC, EXIT
        }

        public class RecorderQuery {
            private Recorder<TStateId> recorder;

            public RecorderQuery(Recorder<TStateId> recorder) {
                this.recorder = recorder;
            }

            private void CheckNext((TStateId state, RecordedAction action) step) {
                if (recorder.recordedSteps.Count == 0) {
                    Assert.Fail($"No recorded steps left. {step} has not happened yet.");
                }
                Assert.AreEqual(recorder.recordedSteps.Dequeue(), step);
            }

            public RecorderQuery Enter(TStateId stateName) {
                CheckNext((stateName, RecordedAction.ENTER));
                return this;
            }

            public RecorderQuery Logic(TStateId stateName) {
                CheckNext((stateName, RecordedAction.LOGIC));
                return this;
            }

            public RecorderQuery Exit(TStateId stateName) {
                CheckNext((stateName, RecordedAction.EXIT));
                return this;
            }

            public void All() {
                if (recorder.recordedSteps.Count != 0) {
                    Assert.Fail($"Too many events happened. Remaining steps: " + recorder.CreateTraceback());
                }
            }

            public void Empty() {
                if (recorder.recordedSteps.Count != 0) {
                    Assert.Fail("Expected nothing to happen. Recorded steps: " + recorder.CreateTraceback());
                }
            }
        }

        private Queue<(TStateId state, RecordedAction action)> recordedSteps;
        private StateWrapper<TStateId, string> tracker;

        // Fluent interface for checking the validity of the steps
        public RecorderQuery Check => new RecorderQuery(this);

        public StateBase<TStateId> TrackedState => Track(new StateBase<TStateId>(false));

        public Recorder() {
            recordedSteps = new Queue<(TStateId state, RecordedAction action)>();
            tracker = new StateWrapper<TStateId, string>(
                beforeOnEnter: s => RecordEnter(s.name),
                beforeOnLogic: s => RecordLogic(s.name),
                beforeOnExit: s => RecordExit(s.name)
            );
        }

        public void RecordEnter(TStateId state) => recordedSteps.Enqueue((state, RecordedAction.ENTER));
        public void RecordLogic(TStateId state) => recordedSteps.Enqueue((state, RecordedAction.LOGIC));
        public void RecordExit(TStateId state) => recordedSteps.Enqueue((state, RecordedAction.EXIT));

        public StateBase<TStateId> Track(StateBase<TStateId> state) {
            return tracker.Wrap(state);
        }

        private string CreateTraceback() {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine();
            foreach (var step in recordedSteps) {
                builder.AppendLine(step.ToString());
            }

            return builder.ToString();
        }

        public void DiscardAll() {
            recordedSteps.Clear();
        }
    }

    public class Recorder : Recorder<string> {

    }
}
