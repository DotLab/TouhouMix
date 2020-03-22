using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TouhouMix {
  public sealed class LanguageOptionFiller : MonoBehaviour {
    public Dropdown dropdown;

    public void Start() {
      var optionList = Levels.GameScheduler.instance.resourceStorage.langOptionList;
      dropdown.options = optionList.Select(x => new Dropdown.OptionData(x.name)).ToList();
      GetComponent<Uif.Binding.MemberBinding>().isEnabled = true;
      GetComponent<Uif.Binding.MemberBinding>().PullSelfValue();
    }
  }
}
