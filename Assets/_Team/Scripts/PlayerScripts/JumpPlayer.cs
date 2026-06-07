using UnityEngine;

public class JumpPlayer : IJump
{
    private float m_durationJump;
    private float m_jumpHeight;
    private float m_timer;

    private Vector3 m_startPos;
    private Vector3 m_endPos;
    private Vector3 m_midPos;

    private bool m_isVaulting;
    private bool m_isCompleted; // Nova flag para evitar o loop

    public JumpPlayer(float durationJump, float jumpHeight)
    {
        m_durationJump = durationJump;
        m_jumpHeight = jumpHeight;
    }

    // Mudamos para retornar bool: true = pulando, false = terminou
    public bool Jump(CharacterController ch, bool isJumping)
    {
        if (!isJumping || m_isCompleted) return false;

        // FASE 1: Ajuste para o ponto inicial
        if (!m_isVaulting)
        {
            Vector3 nextPosition = Vector3.MoveTowards(ch.transform.position, m_startPos, 5 * Time.deltaTime);
            Vector3 motion = nextPosition - ch.transform.position;
            ch.Move(motion);

            if (Vector3.Distance(ch.transform.position, m_startPos) < 0.01f)
            {
                ch.transform.position = m_startPos;
                m_isVaulting = true;
            }
            return true; // Continua executando o Jump
        }

        // FASE 2: O Salto (Vaulting)
        m_timer += Time.deltaTime;
        float t = Mathf.Clamp01(m_timer / m_durationJump);

        if (t < 1f)
        {
            Vector3 m1 = Vector3.Lerp(m_startPos, m_midPos, t);
            Vector3 m2 = Vector3.Lerp(m_midPos, m_endPos, t);
            Vector3 targetPos = Vector3.Lerp(m1, m2, t);

            Vector3 motion = targetPos - ch.transform.position;
            ch.Move(motion);
            return true; // Continua executando
        }
        else
        {
            // FASE 3: Finalização
            Vector3 finalMotion = m_endPos - ch.transform.position;
            ch.Move(finalMotion);
            ch.transform.position = m_endPos;

            m_isVaulting = false;
            m_isCompleted = true; // Impede que o código rode novamente até um novo Initialize
            m_timer = 0;
            return false; // Avisa que ACABOU
        }
    }

    public void JumpInitialize(Vector3 pointEnd, Vector3 pointStart, bool isJumping)
    {
        if (isJumping) return;

        m_startPos = pointStart;
        m_endPos = pointEnd;
        m_midPos = Vector3.Lerp(m_startPos, m_endPos, 0.5f) + Vector3.up * m_jumpHeight;

        m_timer = 0;
        m_isVaulting = false;
        m_isCompleted = false; // Resetamos a flag para um novo pulo
    }
}