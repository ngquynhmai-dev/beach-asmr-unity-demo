using System;
using UnityEngine;

namespace Mai.Beach
{
    /// <summary>Isolated review prototype; does not depend on Perfect Cleaning managers.</summary>
    public sealed class BeachCleaningStepDemo : MonoBehaviour
    {
        [Serializable] public sealed class Step
        {
            public string title, tool, instruction;
            public float clean, repair, foam, wet;
        }
        [Serializable] public sealed class ObjectPlan
        {
            public string key, label;
            public Step[] steps;
        }
        [Serializable] public sealed class Catalog { public ObjectPlan[] objects; }
        [Serializable] public sealed class Target
        {
            public SpriteRenderer renderer;
            public Texture2D cleanTexture;
            public Vector4 damage = new Vector4(-2,-2,.01f,.01f);
            public string validCells;
            [NonSerialized] public int completed;
            [NonSerialized] public float progress;
            [NonSerialized] public bool[] cells;
            [NonSerialized] public Material original, preview;
        }

        [SerializeField] private TextAsset _catalog;
        [SerializeField] private Shader _previewShader;
        [SerializeField] private Target[] _targets;
        [SerializeField] private Camera _camera;
        private Catalog _plans;
        private int _selected;
        private bool _show, _auto, _active;
        private float _autoTime;
        private GUIStyle _title, _body, _button, _small;
        private Rect _panel;
        private string _error;
        private Vector2 _scroll;
        private bool _previousRunInBackground;

        public int SelectedObject { get { return _selected; } }
        public int ObjectCount { get { return _targets == null ? 0 : _targets.Length; } }
        public int CompletedSteps(int i) { return _targets[i].completed; }
        public int StepCount(int i) { return _plans.objects[i].steps.Length; }

        private void Start()
        {
            _previousRunInBackground=Application.runInBackground;
            Application.runInBackground=true;
            try
            {
                _plans = JsonUtility.FromJson<Catalog>(_catalog.text);
                if (_targets == null || _targets.Length != 4 || _plans.objects.Length != 4 || _previewShader == null)
                    throw new InvalidOperationException("Thiếu dữ liệu demo hoặc shader.");
                for (int i=0;i<_targets.Length;i++)
                {
                    var t=_targets[i];
                    if (t.renderer==null || t.cleanTexture==null || _plans.objects[i].steps.Length!=14)
                        throw new InvalidOperationException("Mỗi vật thể phải có sprite, ảnh sạch và đúng 14 bước.");
                    t.cells=new bool[64];
                    t.original=t.renderer.sharedMaterial;
                    t.preview=new Material(_previewShader);
                    t.preview.SetTexture("_DirtyTex",t.renderer.sprite.texture);
                    t.preview.SetTexture("_CleanTex",t.cleanTexture);
                    t.preview.SetVector("_Damage",t.damage);
                    t.renderer.sharedMaterial=t.preview;
                    UpdateVisual(i);
                }
            }
            catch (Exception e) { _error=e.Message; Debug.LogError("Beach demo: "+e.Message,this); }
        }

        private void Update()
        {
            if (_error!=null || _plans==null) return;
            if (Input.GetKeyDown(KeyCode.D)) { _show=!_show; _active=true; }
            if (!_active) return;
            var t=_targets[_selected];
            if (t.completed>=14) return;
            if (_auto)
            {
                _autoTime+=Time.unscaledDeltaTime;
                t.progress=Mathf.Clamp01(_autoTime/1.4f);
                UpdateVisual(_selected);
                if (t.progress>=1) { FinishStep(); _auto=false; }
                return;
            }
            if (!Input.GetMouseButton(0)) return;
            Vector2 mouse=Input.mousePosition;
            Vector2 gui=new Vector2(mouse.x,Screen.height-mouse.y);
            if (_panel.Contains(gui)) return;
            var world=_camera.ScreenToWorldPoint(new Vector3(mouse.x,mouse.y,10));
            var b=t.renderer.bounds;
            if (world.x<b.min.x || world.x>b.max.x || world.y<b.min.y || world.y>b.max.y) return;
            StrokeAtUV(new Vector2(Mathf.InverseLerp(b.min.x,b.max.x,world.x),Mathf.InverseLerp(b.min.y,b.max.y,world.y)));
        }

        public void SelectObject(int index)
        {
            if (_plans==null || index<0 || index>=_targets.Length) return;
            _selected=index; _auto=false; _scroll=Vector2.zero;
        }

        public void StrokeAtUV(Vector2 uv)
        {
            if (_plans==null || _targets[_selected].completed>=14) return;
            var t=_targets[_selected];
            int x=Mathf.Clamp((int)(uv.x*8),0,7), y=Mathf.Clamp((int)(uv.y*8),0,7);
            int index=y*8+x;
            if (!string.IsNullOrEmpty(t.validCells) && t.validCells.Length==64 && t.validCells[index]!='1') return;
            t.cells[index]=true;
            int count=0, valid=0;
            for(int i=0;i<64;i++)
            {
                if(t.cells[i]) count++;
                if(string.IsNullOrEmpty(t.validCells) || t.validCells[i]=='1') valid++;
            }
            t.progress=Mathf.Clamp01(count/Mathf.Max(5f,valid*.38f));
            UpdateVisual(_selected);
            if(t.progress>=1) FinishStep();
        }

        public void PreviewStep()
        {
            if(_plans==null || _targets[_selected].completed>=14) return;
            _show=true; _active=true; _auto=true; _autoTime=0;
        }

        // Explicit review shortcut. Dragging still uses StrokeAtUV and unique surface cells.
        public void CompleteStepForReview()
        {
            if(_plans==null || _targets[_selected].completed>=14) return;
            _targets[_selected].progress=1; FinishStep();
        }

        private void FinishStep()
        {
            var t=_targets[_selected];
            t.completed=Mathf.Min(14,t.completed+1);
            t.progress=0; Array.Clear(t.cells,0,t.cells.Length);
            UpdateVisual(_selected);
        }

        public void ResetObject(int index)
        {
            if(_plans==null || index<0 || index>=_targets.Length) return;
            var t=_targets[index]; t.completed=0; t.progress=0;
            Array.Clear(t.cells,0,t.cells.Length); _auto=false; UpdateVisual(index);
        }

        private void UpdateVisual(int index)
        {
            var t=_targets[index];
            if(t.preview==null) return;
            Step from=t.completed>0 ? _plans.objects[index].steps[t.completed-1] : null;
            Step to=t.completed<14 ? _plans.objects[index].steps[t.completed] : from;
            float p=t.progress;
            t.preview.SetFloat("_Clean",Mathf.Lerp(from==null?0:from.clean,to.clean,p));
            t.preview.SetFloat("_Repair",Mathf.Lerp(from==null?0:from.repair,to.repair,p));
            t.preview.SetFloat("_Foam",Mathf.Lerp(from==null?0:from.foam,to.foam,p));
            t.preview.SetFloat("_Wet",Mathf.Lerp(from==null?0:from.wet,to.wet,p));
        }

        private void OnGUI()
        {
            if(_error!=null) { GUI.Box(new Rect(12,12,420,70),_error); return; }
            if(_plans==null) return;
            float scale=Mathf.Clamp(Screen.width/900f,.65f,1.6f);
            if(_title==null)
            {
                _title=new GUIStyle(GUI.skin.label) { fontStyle=FontStyle.Bold, wordWrap=true };
                _body=new GUIStyle(GUI.skin.label) { wordWrap=true };
                _small=new GUIStyle(GUI.skin.label) { wordWrap=true };
                _button=new GUIStyle(GUI.skin.button) { wordWrap=true };
            }
            _title.fontSize=Mathf.RoundToInt(19*scale); _body.fontSize=Mathf.RoundToInt(15*scale);
            _small.fontSize=Mathf.RoundToInt(12*scale); _button.fontSize=Mathf.RoundToInt(14*scale);
            float width=Mathf.Min(Screen.width-24,420*scale), height=Mathf.Min(Screen.height-76,535*scale);
            if(GUI.Button(new Rect(12,12,Mathf.Min(240*scale,Screen.width-24),40*scale),_show?"Ẩn bảng demo [D]":"DEMO 14 BƯỚC [D]",_button)) { _show=!_show; _active=true; }
            if(!_show) { _panel=Rect.zero; return; }
            _panel=new Rect(12,18+40*scale,width,height);
            GUI.Box(_panel,GUIContent.none);
            float x=_panel.x+12*scale, y=_panel.y+10*scale, inner=width-24*scale;
            GUI.Label(new Rect(x,y,inner,30*scale),"DEMO QUY TRÌNH LÀM SẠCH",_title); y+=33*scale;
            for(int i=0;i<4;i++)
            {
                string label=_plans.objects[i].label+"  "+_targets[i].completed+"/14";
                Color old=GUI.backgroundColor; if(i==_selected) GUI.backgroundColor=new Color(.35f,.8f,.85f);
                if(GUI.Button(new Rect(x+(i%2)*(inner*.5f),y+(i/2)*36*scale,inner*.5f-4*scale,32*scale),label,_button)) SelectObject(i);
                GUI.backgroundColor=old;
            }
            y+=78*scale;
            var target=_targets[_selected];
            bool done=target.completed>=14;
            var step=_plans.objects[_selected].steps[Mathf.Min(13,target.completed)];
            GUI.Label(new Rect(x,y,inner,55*scale),done?"HOÀN TẤT 14/14":("Bước "+(target.completed+1)+"/14 · "+step.title),_title); y+=58*scale;
            GUI.Label(new Rect(x,y,inner,45*scale),done?"Có thể chọn vật thể khác hoặc làm lại.":("Dụng cụ: "+step.tool+"\n"+step.instruction),_body); y+=52*scale;
            GUI.Box(new Rect(x,y,inner,14*scale),GUIContent.none);
            Color color=GUI.color; GUI.color=new Color(.2f,.85f,.65f);
            GUI.DrawTexture(new Rect(x,y,inner*((target.completed+target.progress)/14f),14*scale),Texture2D.whiteTexture); GUI.color=color; y+=24*scale;
            GUI.enabled=!done && !_auto;
            if(GUI.Button(new Rect(x,y,inner*.58f,34*scale),"Demo bước hiện tại",_button)) PreviewStep();
            GUI.enabled=true;
            if(GUI.Button(new Rect(x+inner*.6f,y,inner*.4f,34*scale),"Làm lại",_button)) ResetObject(_selected);
            y+=41*scale;
            GUI.Label(new Rect(x,y,inner,38*scale),"Hoặc ẩn bảng rồi giữ và kéo chuột/ngón tay trên vật thể để thực hiện bước.",_small); y+=43*scale;
            float scrollHeight=Mathf.Max(35*scale,_panel.yMax-y-50*scale);
            _scroll=GUI.BeginScrollView(new Rect(x,y,inner,scrollHeight),_scroll,new Rect(0,0,inner-22*scale,14*25*scale));
            for(int i=0;i<14;i++) GUI.Label(new Rect(0,i*25*scale,inner-25*scale,25*scale),(i<target.completed?"✓ ":(i==target.completed?"▶ ":"  "))+(i+1)+". "+_plans.objects[_selected].steps[i].title,_small);
            GUI.EndScrollView();
            GUI.Label(new Rect(x,_panel.yMax-44*scale,inner,40*scale),"Minh họa chuyển lớp + dụng cụ. Mask riêng từng loại vết bẩn sẽ được hoàn thiện sau khi duyệt quy trình.",_small);
        }

        private void OnDestroy()
        {
            Application.runInBackground=_previousRunInBackground;
            if(_targets==null) return;
            foreach(var t in _targets)
            {
                if(t.renderer!=null && t.original!=null) t.renderer.sharedMaterial=t.original;
                if(t.preview!=null) Destroy(t.preview);
            }
        }
    }
}
