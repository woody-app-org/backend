package com.woody.backend.infraestructure.entity;

import java.util.Objects;

import jakarta.persistence.Embeddable;

@Embeddable
public class UserFollowsId {
    private long idUserFollowing;
    private long idUserFollower;

    public UserFollowsId(){}

    public UserFollowsId(long idUserFollowing, long idUserFollower) {
        this.idUserFollowing = idUserFollowing;
        this.idUserFollower = idUserFollower;
    }

    public long getIdUserFollowing() {
        return idUserFollowing;
    }

    public void setIdUserFollowing(long idUserFollowing) {
        this.idUserFollowing = idUserFollowing;
    }

    public long getIdUserFollower() {
        return idUserFollower;
    }

    public void setIdUserFollower(long idUserFollower) {
        this.idUserFollower = idUserFollower;
    }

    @Override
    public boolean equals(Object o){
        if (this == o) return true;
        if (!(o instanceof UserFollowsId that)) return false;
        return Objects.equals(idUserFollower, that.idUserFollower) && Objects.equals(idUserFollowing, that.idUserFollowing);
    }

    @Override
    public int hashCode() {
        return Objects.hash(idUserFollower, idUserFollowing);
    }
}
