package com.woody.backend.infraestructure.entity;

import java.time.LocalDate;

import jakarta.persistence.EmbeddedId;
import jakarta.persistence.Entity;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.MapsId;

@Entity
public class UserFollowsEntity {
    @EmbeddedId
    private UserFollowsId id;

    @MapsId("idUserFollowing")
    @ManyToOne
    private UserEntity user_following;
    
    @MapsId("idUserFollower")
    @ManyToOne
    private UserEntity user_follower;

    private LocalDate created_at;

    public UserFollowsEntity(){}

    public UserFollowsEntity(UserFollowsId id, LocalDate created_at){
        this.id = id;
        this.created_at = created_at;
    }

    public UserEntity getUser_following() {
        return user_following;
    }

    public void setUser_following(UserEntity user_following) {
        this.user_following = user_following;
    }

    public UserEntity getUser_follower() {
        return user_follower;
    }

    public void setUser_follower(UserEntity user_follower) {
        this.user_follower = user_follower;
    }

    public LocalDate getCreated_at() {
        return created_at;
    }

    public void setCreated_at(LocalDate created_at) {
        this.created_at = created_at;
    }

    public UserFollowsId getId() {
        return id;
    }
}
