using System;
using UnityEngine;
using UnityEngine.UIElements;

// Painel Settings: as 3 barras de volume (Geral, Música e Efeitos, de 0 a 100) com o número ao lado.
// Usado pelo menu principal (MenuManager) e pelo menu de pausa (MenuDePausa). Os dois UXML usam os mesmos
// names: Slider_Geral/Valor_Geral, Slider_Musica/Valor_Musica e Slider_Efeitos/Valor_Efeitos.
public sealed class PainelDeVolumes
{
    // Escala dos sliders de volume (0 a 100); o ConfiguracaoDeAudio usa 0 a 1.
    private const int SliderMaximo = 100;

    private readonly LinhaDeVolume geral;
    private readonly LinhaDeVolume musica;
    private readonly LinhaDeVolume efeitos;

    public PainelDeVolumes(VisualElement root)
    {
        geral = LinhaDeVolume.Criar(root, "Slider_Geral", "Valor_Geral", volume => ConfiguracaoDeAudio.Geral = volume);
        musica = LinhaDeVolume.Criar(root, "Slider_Musica", "Valor_Musica", volume => ConfiguracaoDeAudio.Musica = volume);
        efeitos = LinhaDeVolume.Criar(root, "Slider_Efeitos", "Valor_Efeitos", volume => ConfiguracaoDeAudio.Efeitos = volume);
    }

    // Coloca nas barras os volumes atuais (chamar ao abrir o painel).
    public void Mostrar()
    {
        geral?.Mostrar(ConfiguracaoDeAudio.Geral);
        musica?.Mostrar(ConfiguracaoDeAudio.Musica);
        efeitos?.Mostrar(ConfiguracaoDeAudio.Efeitos);
    }

    // Um slider (0 a 100) com o número ao lado, ligado a um volume do ConfiguracaoDeAudio.
    private sealed class LinhaDeVolume
    {
        private readonly SliderInt slider;
        private readonly Label valor;

        private LinhaDeVolume(SliderInt slider, Label valor, Action<float> aoMudar)
        {
            this.slider = slider;
            this.valor = valor;

            slider.RegisterValueChangedCallback(evt =>
            {
                valor.text = evt.newValue.ToString();
                aoMudar(evt.newValue / (float)SliderMaximo);
            });
        }

        // null se o slider ou o número não existirem no UXML (avisa no Console).
        public static LinhaDeVolume Criar(VisualElement root, string nomeSlider, string nomeValor, Action<float> aoMudar)
        {
            SliderInt slider = root.Q<SliderInt>(nomeSlider);
            Label valor = root.Q<Label>(nomeValor);
            if (slider == null || valor == null)
            {
                Debug.LogWarning("PainelDeVolumes: '" + nomeSlider + "' ou '" + nomeValor + "' não encontrado. Confira se o PanelRenderer usa o UXML atualizado.");
                return null;
            }

            return new LinhaDeVolume(slider, valor, aoMudar);
        }

        // Mostra o volume atual (0 a 1) sem disparar o evento de mudança.
        public void Mostrar(float volume)
        {
            int inteiro = Mathf.RoundToInt(Mathf.Clamp01(volume) * SliderMaximo);
            slider.SetValueWithoutNotify(inteiro);
            valor.text = inteiro.ToString();
        }
    }
}
