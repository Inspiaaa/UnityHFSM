using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FSM {
    public static class TransitionOnKey {
        
        public class Down : TransitionBase {
            private KeyCode keyCode;

            public Down(
                    string from, 
                    string to, 
                    KeyCode key,
                    bool forceInstantly = false) : base(from, to, forceInstantly) {

                keyCode = key;
            }

            public override bool ShouldTransition() {
                return Input.GetKey(keyCode);
            }
        }

        public class Release : TransitionBase {
            private KeyCode keyCode;

            public Release(
                    string from, 
                    string to, 
                    KeyCode key,
                    bool forceInstantly = false) : base(from, to, forceInstantly) {

                keyCode = key;
            }

            public override bool ShouldTransition() {
                return Input.GetKeyUp(keyCode);
            }
        }

        public class Press : TransitionBase {
            private KeyCode keyCode;

            public Press(
                    string from, 
                    string to, 
                    KeyCode key,
                    bool forceInstantly = false) : base(from, to, forceInstantly) {

                keyCode = key;
            }

            public override bool ShouldTransition() {
                return Input.GetKeyDown(keyCode);
            }
        }

        public class Up : TransitionBase {
            private KeyCode keyCode;

            public Up(
                    string from, 
                    string to, 
                    KeyCode key,
                    bool forceInstantly = false) : base(from, to, forceInstantly) {

                keyCode = key;
            }

            public override bool ShouldTransition() {
                return ! Input.GetKey(keyCode);
            }
        }
    }
}
