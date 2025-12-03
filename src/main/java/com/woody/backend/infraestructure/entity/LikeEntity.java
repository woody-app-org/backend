package com.woody.backend.infraestructure.entity;

import java.time.LocalDate;

import com.woody.backend.infraestructure.entity.embeddedIds.LikeId;

import jakarta.persistence.EmbeddedId;
import jakarta.persistence.Entity;
import jakarta.persistence.ManyToOne;
import jakarta.persistence.MapsId;

@Entity
public class LikeEntity {
    @EmbeddedId
    private LikeId id;

    @MapsId("userId")
    @ManyToOne
    private UserEntity user;

    @MapsId("postId")
    @ManyToOne
    private PostEntity post;

    private LocalDate created_at;

    public LikeEntity(){}

    public LikeEntity(LikeId id, LocalDate created_at) {
        this.id = id;
        this.created_at = created_at;
    }

    public UserEntity getUser() {
        return user;
    }

    public void setUser(UserEntity user) {
        this.user = user;
    }

    public PostEntity getPost() {
        return post;
    }

    public void setPost(PostEntity post) {
        this.post = post;
    }

    public LocalDate getCreated_at() {
        return created_at;
    }

    public void setCreated_at(LocalDate created_at) {
        this.created_at = created_at;
    }

    public LikeId getId() {
        return id;
    }
}
