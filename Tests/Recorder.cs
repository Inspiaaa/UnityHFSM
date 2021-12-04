
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using FSM;

namespace FSM.Tests
{
    public class Recorder {
        private enum RecordedAction {
            ENTER, LOGIC, EXIT
        }

        public class RecorderQuery {
            private Recorder recorder;

            public RecorderQuery(Recorder recorder) {
                this.recorder = recorder;
            }

            private void CheckNext((string state, RecordedAction action) step) {
                if (recorder.recordedSteps.Count == 0) {
                    Assert.Fail($"No recorded steps left. {step} has not happened yet.");
                }
                Assert.AreEqual(recorder.recordedSteps.Dequeue(), step);
            }

            public RecorderQuery Enter(string stateName) {
                CheckNext((stateName, RecordedAction.ENTER));
                return this;
            }

            public RecorderQuery Logic(string stateName) {
                CheckNext((stateName, RecordedAction.LOGIC));
                return this;
            }

            public RecorderQuery Exit(string stateName) {
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

        private Queue<(string state, RecordedAction action)> recordedSteps;
        private StateWrapper tracker;

        // Fluent interface for checking the validity of the steps
        public RecorderQuery Check => new RecorderQuery(this);

        public StateBase<string> TrackedState => Track(new StateBase<string>(false));

        public Recorder() {
            recordedSteps = new Queue<(string state, RecordedAction action)>();
            tracker = new StateWrapper(
                beforeOnEnter: s => RecordEnter(s.name),
                beforeOnLogic: s => RecordLogic(s.name),
                beforeOnExit: s => RecordExit(s.name)
            );
        }

        public void RecordEnter(string state) => recordedSteps.Enqueue((state, RecordedAction.ENTER));
        public void RecordLogic(string state) => recordedSteps.Enqueue((state, RecordedAction.LOGIC));
        public void RecordExit(string state) => recordedSteps.Enqueue((state, RecordedAction.EXIT));

        public StateBase<string> Track(StateBase<string> state) {
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
}
