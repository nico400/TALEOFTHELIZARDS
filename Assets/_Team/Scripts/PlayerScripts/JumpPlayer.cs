using UnityEngine;

public class JumpPlayer : IJump
{
    float durationJump;
    float jumpHeight;
    float timer;

    Vector3 startPos;
    Vector3 endPos;
    Vector3 midPos;

    bool isVaulting;
    bool isCompleted; // Nova flag para evitar o loop

    public JumpPlayer(float durationJump, float jumpHeight)
    {
        this.durationJump = durationJump;
        this.jumpHeight = jumpHeight;
    }

    // Mudamos para retornar bool: true = pulando, false = terminou
    public bool Jump(CharacterController ch, bool isJumping)
    {
        if (!isJumping || isCompleted) return false;

        // FASE 1: Ajuste para o ponto inicial
        if (!isVaulting)
        {
            Vector3 nextPosition = Vector3.MoveTowards(ch.transform.position, startPos, 5 * Time.deltaTime);
            Vector3 motion = nextPosition - ch.transform.position;
            ch.Move(motion);

            if (Vector3.Distance(ch.transform.position, startPos) < 0.01f)
            {
                ch.transform.position = startPos;
                isVaulting = true;
            }
            return true; // Continua executando o Jump
        }

        // FASE 2: O Salto (Vaulting)
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / durationJump);

        if (t < 1f)
        {
            Vector3 m1 = Vector3.Lerp(startPos, midPos, t);
            Vector3 m2 = Vector3.Lerp(midPos, endPos, t);
            Vector3 targetPos = Vector3.Lerp(m1, m2, t);

            Vector3 motion = targetPos - ch.transform.position;
            ch.Move(motion);
            return true; // Continua executando
        }
        else
        {
            // FASE 3: Finalização
            Vector3 finalMotion = endPos - ch.transform.position;
            ch.Move(finalMotion);
            ch.transform.position = endPos;

            isVaulting = false;
            isCompleted = true; // Impede que o código rode novamente até um novo Initialize
            timer = 0;
            return false; // Avisa que ACABOU
        }
    }

    public void JumpInitialize(Vector3 pointEnd, Vector3 pointStart, bool isJumping)
    {
        if (isJumping) return;

        startPos = pointStart;
        endPos = pointEnd;
        midPos = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * jumpHeight;

        timer = 0;
        isVaulting = false;
        isCompleted = false; // Resetamos a flag para um novo pulo
    }
}