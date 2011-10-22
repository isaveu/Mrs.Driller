using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Block : MonoBehaviour {
    public enum Type {
        Blue = 0, Green, Pink, Yellow,
        // Air, Hard,
        Empty
    }

    public class Group {
        public int Count {
            get { return this.blocks.Count; }
        }

        public bool unbalance = false;

        public bool blinking {
            get { return this.blink != null; }
        }

        HashSet<Block> blocks;

        BlockController blockController;

        IEnumerator blink;

        public Group(BlockController blockController) {
            this.blockController = blockController;
            this.blocks = new HashSet<Block>();
        }

        public void Grouping(Block block) {
            if (!Add(block)) return;

            // 上下左右
            foreach (Direction d in System.Enum.GetValues(typeof(Direction))) {
                if (blockController.Collision(block.pos, d) == block.type) {
                    Grouping(blockController.NextBlock(block.pos, d));
                }
            }
        }

        public void BlinkStart() {
            this.blink = GetBlinkEnumerator();
        }

        public HashSet<Group> LookUpUpperGroups() {
            HashSet<Group> result = new HashSet<Group>();

            foreach (Block member in this.blocks) {
                if (member.pos.y < 1) continue;

                Block upperBlock = blockController.NextBlock(member.pos, Direction.Up);
                if (upperBlock != null && upperBlock.group != this) {
                    result.Add(upperBlock.group);
                }
            }

            return result;
        }

        public HashSet<Group> LookUpUnbalanceGroups() {
            HashSet<Group> result  = new HashSet<Group>();
            HashSet<Group> history = new HashSet<Group>();

            LookUpUnbalanceGroupsRecursive(result, history, this);

            return result;
        }

        public IEnumerator<Block> GetEnumerator() {
            foreach (Block block in this.blocks) {
                yield return block;
            }
        }

        bool Add(Block block) {
            block.group = this;
            return blocks.Add(block);
        }

        void LookUpUnbalanceGroupsRecursive(HashSet<Group> result,
                                            HashSet<Group> history,
                                            Group group) {
            history.Add(group);

            // 自分が乗っかっているグループ調べる
            foreach (Block member in group.blocks) {
                if (member.pos.y > blockController.numBlockRows - 2) continue;

                Block underBlock = blockController.NextBlock(member.pos, Direction.Down);
                if (underBlock != null && underBlock.group != group) {
                    if (history.Contains(underBlock.group)) {
                        if (!underBlock.group.unbalance) return;
                    } else {
                        foreach (Block underMember in underBlock.group) {
                            Block underUnderBlock = blockController.NextBlock(
                                underMember.pos, Direction.Down
                            );
                            if (underUnderBlock != null &&
                                !history.Contains(underUnderBlock.group)) {
                                return;
                            }
                        }
                        LookUpUnbalanceGroupsRecursive(result, history, underBlock.group);
                    }
                }
            }

            group.unbalance = true;
            result.Add(group);

            // // 自分に乗っているグループ調べる
            foreach (Group upperBlock in group.LookUpUpperGroups()) {
                LookUpUnbalanceGroupsRecursive(result, history, upperBlock);
            }
        }

        IEnumerator GetBlinkEnumerator() {
            float beforeBlink = Time.time;

            while (Time.time - beforeBlink < blockController.blinkTime) {
                float alpha = Mathf.Sin(Time.time * 100.0f);
                foreach (Block member in this.blocks) {
                    Color color = member.renderer.material.color;
                    color.a = alpha;
                    member.renderer.material.color = color;
                }
                yield return true;
            }

            foreach (Block member in this.blocks) {
                Color color = member.renderer.material.color;
                color.a = 0;
                member.renderer.material.color = color;
            }
        }
    }

    public Material[] blockMaterials;

    public Vector2 pos;
    public Group group;

    Type _type;
    public Type type {
        get {
            return this._type;
        }

        set {
            this._type = value;
            if (value == Type.Empty) {
                renderer.enabled = false;
            } else {
                renderer.enabled = true;
                renderer.material = this.blockMaterials[(int)value];
            }
        }
    }

    public bool shaking {
        get { return this.shake != null;  }
    }

    public bool dropping {
        get { return this.drop != null; }
    }

    public bool unbalance {
        get { return this.shaking || this.dropping; }
    }

    IEnumerator shake;
    IEnumerator drop;

    public override string ToString() {
        return "type:" + this.type + " pos:" + pos;
    }

    public void ShakeStart(float shakeTime) {
        this.shake = GetShakeEnumerator(shakeTime);
    }

    public void DropStart(float gravity) {
        this.drop = GetDropEnumerator(gravity);
    }

    public void DropEnd() {
        this.drop = null;
    }

    public void MoveNext() {
        if (this.shake != null && !this.shake.MoveNext()) {
            this.shake = null;
        } else if (this.drop != null) {
            this.drop.MoveNext();
        }
    }
    
    IEnumerator GetShakeEnumerator(float shakeTime) {
        float beforeShake = Time.time;
        float beforeX = pos.x;

        while (Time.time - beforeShake < shakeTime) {
            pos.x += Mathf.Sin(Time.time * 50.0f) / 30.0f;
            yield return true;
        }
        pos.x = beforeX;
    }

    IEnumerator GetDropEnumerator(float gravity) {
        while (true) {
            float gravityPerFrame = gravity * Time.deltaTime;
            this.pos.y += gravityPerFrame;
            yield return true;}
    }
} 