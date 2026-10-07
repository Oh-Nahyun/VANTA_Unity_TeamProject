
// 보스2 공격이 플레이어에게 요구하는 회피(QTE) 종류. 종류마다 PlayerAvoid 의 다른 시작 함수를 부른다.
public enum Boss2AvoidType
{
    // 피하기만 한다. PlayerAvoid.StartAvoidInteraction (Space, 성공 시 펫 공격)
    Dodge,

    // 피하고 보스에게 반격한다. PlayerAvoid.StartAvoidAttackInteraction (마우스 우클릭, 성공 시 보스 반격)
    Counter
}
